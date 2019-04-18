#include "mpi.h"
#include "Image.h"
#include <complex>
#include <iostream>
#include <vector>
#include <chrono>

typedef std::complex<double> Complex;

//region of fractal that is being displayed
const double xmin = -1.5;
const double xmax = 0.5;
const double ymin = -1;
const double ymax = 1;
int maxHeight;
std::vector<uint8_t> globalData;

struct workLoad {
	int w, h;
	int stride, miter;
	int y;
};

void splitWork(std::vector<uint8_t>& data, int w, int h, int stride, int maxiter, int numTasks);
void compute(int rank);
std::vector<uint8_t> getMessage(int rank);
//void compute(std::vector<uint8_t>& data, int w, int h, int stride, int maxiter);
int iterationsToInfinity(double px, double py, int maxiter);
void mapColor(int k, int MAX_ITERS, uint8_t rgb[3]);

int main(int argc, char* argv[])
{
	MPI_Init(&argc, &argv);
	int rank;
	MPI_Comm_rank(MPI_COMM_WORLD, &rank);	//get tasks rank

	if (rank == 0) //main thread
	{
		const int IMAGE_WIDTH = 1024;
		const int IMAGE_HEIGHT = 1024;

		//fractal iterations is pow(2,maxiter_)
		int maxiter_ = 8;
		int numTasks;

		auto start_time = std::chrono::system_clock::now();
		std::vector<uint8_t> idata(IMAGE_WIDTH * IMAGE_HEIGHT * 3);
		MPI_Comm_size(MPI_COMM_WORLD, &numTasks);	//Get number of tasks
		numTasks -= 1;
		splitWork(idata, IMAGE_WIDTH, IMAGE_HEIGHT, IMAGE_WIDTH * 3, (1 << maxiter_), numTasks);
		
		auto end_time = std::chrono::system_clock::now();
		Image img(IMAGE_WIDTH, IMAGE_HEIGHT, "RGB8");
		memcpy(img.pixels(), idata.data(), idata.size());
		img.writePng("fractal.png");
		auto msec = std::chrono::duration_cast<std::chrono::milliseconds>(end_time - start_time).count();
		std::cout << "Total time: " << msec / 1000.0 << " seconds\n";
	}
	else			//task
		compute(rank);

    return 0;
}

void splitWork(std::vector<uint8_t>& data, int w, int h, int stride, int maxiter, int numTasks)
{
	int sizeOfData = data.size() * sizeof(data[0]);
	globalData.clear(); globalData.resize(sizeOfData);
	std::copy(data.begin(), data.end(), globalData.begin());
	std::cout << "starting send numTasks: " << numTasks << std::endl;

	int heightMul = h / numTasks;
	maxHeight = h;
	for (int i = 1; i <= numTasks; i++)
	{
		workLoad* task = new workLoad();
		task->w = w; task->h = heightMul * (i);									//set width of image, and height to do work on
		task->miter = maxiter; task->stride = stride;							//set maxiter and stride
		task->y = (h / numTasks) * i-1;											//set starting x and starting y
		
		MPI_Send(&task, sizeof(task), MPI_BYTE, i, 0, MPI_COMM_WORLD);			//send size to work on
	}
	std::vector<uint8_t> newData;
	newData.resize(sizeOfData);
	for (int i = 1; i <= numTasks; i++)
	{
		char msg[4];
		MPI_Status status;
		int sizeOfData;

		MPI_Probe(0, 0, MPI_COMM_WORLD, &status);
		MPI_Get_count(&status, MPI_BYTE, &sizeOfData);
		MPI_Recv(msg, sizeOfData, MPI_CHAR, 0, 0, MPI_COMM_WORLD, &status);
	}

	data = newData;
	data.clear(); data.resize(sizeOfData);
	std::copy(globalData.begin(), globalData.end(), data.begin());
	std::cout << "Done" << std::endl;
}

//this is where the actual Mandelbrot computation takes place.
void compute(int rank)
{
	std::cout << rank << std::endl;
	workLoad task;
	int sizeOfData;
	MPI_Status status;
	MPI_Recv(&task, sizeof(int)*5, MPI_BYTE, rank, 0, MPI_COMM_WORLD, &status);
	std::cout << "got data" << std::endl;

	//std::vector<uint8_t>& data, int w, int h, int stride, int maxiter
	int maxiter = task.miter, stride = task.stride;
	int h = task.h, w = task.w;
    auto deltaY = (ymax - ymin) / (double)(maxHeight);
    auto deltaX = (xmax - xmin) / (double)(w);
    int x, y;
    double px, py;
    uint8_t rgb[3];
    for(y = task.y,py = ymin; y < h; y++,py += deltaY) {
        int idx = y * stride;
        for(x = 0,px = xmin; x < w; x++,px += deltaX) {
            int iter = iterationsToInfinity(px, py, maxiter);
            mapColor(iter, maxiter, rgb);
            globalData[idx++] = rgb[0];
			globalData[idx++] = rgb[1];
			globalData[idx++] = rgb[2];
        }
    }
	MPI_Send("Done", sizeof(char)*4, MPI_CHAR, 0, 0, MPI_COMM_WORLD);		//send data back
}

//for point px,py, return the number of iterations it takes
//to get to infinity.
int iterationsToInfinity(double px, double py, int maxiter)
{
    Complex c(px, py);
    Complex z(0, 0);
    for(int k = 0; k < maxiter; k++) {
        z = z * z;
        z = z + c;
        if(z.real() * z.real() + z.imag() * z.imag() > 4) {
            return k;
        }
    }
    return maxiter;
}

//map an iteration count to a color
void mapColor(int k, int MAX_ITERS, uint8_t rgb[3])
{
    // Map a color to an RGB value
    // When k=0, returns red
    // As k approaches MAX_ITERS, the returned color
    // will proceed through orange, yellow, green, blue, and purple
    // If k >= MAX_ITERS, the returned color is black.
    // Returned values: red, green, blue, in the range 0...255
    //N. Schaller's algorithm to map
    //HSV to RGB values.
    //http://www.cs.rit.edu/~ncs/color/t_convert.html

    double s = 0.8;   //saturation
    double v = 0.95;  //value
    double h = k / (double)MAX_ITERS * 360.0 / 60.0;       //hue

    if(h >= 6)
        v = 0;

    int ipart = (int)h;
    double fpart = h - ipart;
    double A = v * (1 - s);
    double B = v * (1 - s * fpart);
    double C = v * (1 - s * (1 - fpart));
    double r, g, b;

    if(ipart == 0) {
        r = v;
        g = C;
        b = A;
    } else if(ipart == 1) {
        r = B;
        g = v;
        b = A;
    } else if(ipart == 2) {
        r = A;
        g = v;
        b = C;
    } else if(ipart == 3) {
        r = A;
        g = B;
        b = v;
    } else if(ipart == 4) {
        r = C;
        g = A;
        b = v;
    } else {
        r = v;
        g = A;
        b = B;
    }
    rgb[0] = (uint8_t)(r * 255);
    rgb[1] = (uint8_t)(g * 255);
    rgb[2] = (uint8_t)(b * 255);
}
