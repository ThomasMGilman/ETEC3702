#include "mpi.h"
#include <iostream>
#include <thread>
#include <cstdlib>

using namespace std;

int main(int argc, char* argv[])
{
    MPI_Init(&argc, &argv);
    int rank;
    MPI_Comm_rank(MPI_COMM_WORLD, &rank);
    if( rank == 0 ){
        int numTasks;
        MPI_Comm_size(MPI_COMM_WORLD, &numTasks);
        while(true){
            for(int i=1;i<numTasks;++i){
                int junk;
                MPI_Status status;
                MPI_Recv(&junk, 1, MPI_INT, i, MPI_ANY_TAG, MPI_COMM_WORLD, &status);
                cout << "Task " << i << " is alive\n";
            }
        }
    }
    else{
        while(true){
            int junk=1;
            cout << rank << " sending...\n";
            MPI_Send(&junk,1,MPI_INT,0,0,MPI_COMM_WORLD);
            int st = rand() & 0xfff;
            this_thread::sleep_for(std::chrono::milliseconds(st));
        }
    }
        
    MPI_Finalize();
    return 0;
}
