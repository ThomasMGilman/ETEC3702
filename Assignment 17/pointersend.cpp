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
        string s;
        MPI_Status status;
        MPI_Recv(&s, 1, MPI_CHAR, 1, MPI_ANY_TAG, MPI_COMM_WORLD, &status);
        cout << "We got: " << s << "\n";
    }
    else{
        string s = "Hello, world!";
        MPI_Send(&s,s.length(),MPI_CHAR,0,0,MPI_COMM_WORLD);
    }
        
    MPI_Finalize();
    return 0;
}
