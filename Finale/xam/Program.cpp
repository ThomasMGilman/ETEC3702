
#include <iostream>
#include <vector>
#include <string>
#include <stdexcept>
#include <fstream>

#pragma pack(push,1)
struct TGAHeader{
    uint8_t commentSize; //can be zero
    uint8_t colorType; //0
    uint8_t compression; //2=none, 10=compressed
    uint8_t colorMap[5]; //unused
    uint16_t origin[2]; //0,0
    uint16_t width, height;
    uint8_t bitsPerPixel; //24=BGR, 32=BGRA
    uint8_t descriptor; //0 for BGR; 8 for BGRA
    char comment[0]; //variable size
};
struct Pixel{
    uint8_t b,g,r;
    bool operator==(const Pixel& p2){
        return r==p2.r && g==p2.g && b==p2.b;
    }
};
#pragma pack(pop)

void compressIt(std::vector<Pixel>& ipix, std::vector<uint8_t>& opix)
{
    for(auto p : ipix ){
        opix.push_back( 0 );
        opix.push_back( p.b );
        opix.push_back( p.g );
        opix.push_back( p.r );
    }
}
int main(int argc, char* argv[])
{
    std::string filename = "in.tga";
    std::ifstream ifile(filename, std::ios::binary);
    if( !ifile.good() )
        throw std::runtime_error("No input file: in.tga");
        
    TGAHeader hdr;
    ifile.read( (char*) &hdr, sizeof(hdr) );
    if( hdr.commentSize > 0 ){
        std::vector<char> comment(hdr.commentSize);
        ifile.read( comment.data(), comment.size() );
    }
    std::vector<Pixel> pix(hdr.width*hdr.height);
    ifile.read( (char*)pix.data(), pix.size()*sizeof(pix[0]));

    std::cout << "Read TGA file\n";
    if( hdr.colorType != 0 )
        throw std::runtime_error("Bad colorType");
    if( hdr.compression != 2 )
        throw std::runtime_error("Can only use uncompressed input");
    if( hdr.bitsPerPixel != 24 )
        throw std::runtime_error("Can only use BGR (24 bit)");
    if( hdr.descriptor != 0  )
        throw std::runtime_error("Can only use BGR (descriptor 0)");
    
    std::vector<uint8_t> opix;
    compressIt(pix, opix);

    hdr.commentSize = 0;
    hdr.compression = 10;
    
    std::ofstream ofile("out.tga");
    
    if(!ofile.good() )
        throw std::runtime_error("No output!");
        
    ofile.write( (char*) &hdr, sizeof(hdr) );
    ofile.write( (char*) opix.data(), opix.size() );
    std::cout << "Done!\n";
    return 0;
}

