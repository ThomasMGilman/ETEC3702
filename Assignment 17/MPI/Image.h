// jh 2018
//02-jul-2018: Tested with Windows: OK
//05-oct-2018: Tested with Linux: OK

#pragma once

//Note: Must link with windowscodecs.lib

#include <Windows.h>
#include <cstdint>
#include <stdexcept>
#include <vector>
#include <string>
#include <cstdint>
#include <fstream>
#include <iostream>
#include <cstring>
#include <algorithm>
#include <utility>
#include <cassert>
#include <cmath>

#ifdef WIN32
#include <windows.h>
#include <wincodec.h>
#else
#include <turbojpeg.h>
#include <png.h>
#endif


class Image {
    
public:
    /**Load PNG or JPEG file*/
    Image(std::string filename){
        std::ifstream in(filename,std::ios::binary);
        if( !in.good() )
            throw std::runtime_error("Could not open image file "+filename);
        in.seekg(0,std::ios::end);
        auto sz = in.tellg();
        in.seekg(0);
        std::vector<char> D(sz);
        in.read( D.data(), D.size() );
        if( in.fail() )
            throw std::runtime_error("Could not read image file "+filename);
        
        initialize(D.data(), (unsigned)D.size());
    }
    
    /** Initialize from PNG or JPEG data*/
    Image(const void* data, unsigned size){
        initialize(data,size);
    }
    
    /** Initialize from PNG or JPEG data*/
    template<typename T>
    Image(const std::vector<T>& v){
        initialize(v.data(), v.size()*sizeof(T));
    }
    
    /** Create an empty image of the given size and format*/
    Image(unsigned w, unsigned h, std::string fmt){
        if( fmt == "RGB8" ) // || fmt == "BGR8" )
            Bpp=3;
        else if( fmt == "RGBA8" ) //|| fmt == "BGRA8" )
            Bpp=4;
        else if( fmt == "RGB16" )
            Bpp = 6;
        else if( fmt == "RGBA16" )
            Bpp = 8;
        else
            throw new std::runtime_error("Unknown format");
            
        this->w=w;
        this->h=h;
        this->fmt=fmt;
        pixbuff.resize(w*h*Bpp+31);
    }
    
    unsigned width() const {
        return w;
    }
    
    unsigned height() const {
        return h;
    }
    
    unsigned bytesPerPixel() const {
        return Bpp;
    }
    
    void flipVertically(){
        using namespace std;
        unsigned bytesPerRow = width()*bytesPerPixel();
        vector<char> tmp(bytesPerRow);
        char* sp = (char*) pixels();
        char* dp = (char*) pixels();
        dp += bytesPerRow * (height()-1);
        unsigned H=height()/2;
        for(unsigned y=0;y<H;++y){
            memcpy(tmp.data(),sp,bytesPerRow);
            memcpy(sp,dp,bytesPerRow);
            memcpy(dp,tmp.data(),bytesPerRow);
            sp += bytesPerRow;
            dp -= bytesPerRow;
        }
    }
            
    #pragma pack(push,1)
    struct RGB8Pixel{
        std::uint8_t r,g,b;
    };
    struct RGBA8Pixel{
        std::uint8_t r,g,b,a;
    };
    struct RGB16Pixel{
        std::uint16_t r,g,b;
    };
    struct RGBA16Pixel{
        std::uint16_t r,g,b,a;
    };
    #pragma pack(pop)
    
    RGBA8Pixel sample(float s, float t){
        //float s1=s, t1=t;
        if( s < 0 )
            s = 1.0f + (s - int(s));
        if( t < 0 )
            t = 1.0f + (t - int(t));
            
        s = std::fmod(s,1.0f);
        t = std::fmod(t,1.0f);
        //std::cout << s1 << "->" << s << "   " << t1 << " -> " << t << "\n";
        
        unsigned x = unsigned( 0.5f + s * w);
        unsigned y = unsigned( 0.5f + t * h);
        if( x >= w ) x=w-1;
        if( x < 0 ) x = 0;
        if( y >= h ) y=h-1;
        if( y < 0 ) y = 0;
        std::uint8_t* p = pixels();
        p += y * Bpp * w;
        p += x * Bpp;
        RGBA8Pixel q;
        q.r = p[0];
        q.g = p[1];
        q.b = p[2];
        q.a = p[3];
        return q;
    }
        
        
    void setPixel(unsigned x, unsigned y, std::uint8_t r,   
            std::uint8_t g, std::uint8_t b, std::uint8_t a=255){
        
        unsigned idx = y*w+x;
        
        switch(Bpp){
            case 3:
            {
                //rgb8 
                //~ std::assert(fmt == "RGB8");
                RGB8Pixel* p = (RGB8Pixel*)pixels();
                p[idx].r=r;
                p[idx].g=g;
                p[idx].b=b;
                break;
            }
            case 4:
            {
                //rgba8
                //~ std::assert(fmt == "RGBA8");
                RGBA8Pixel* p = (RGBA8Pixel*)pixels();
                p[idx].r=r;
                p[idx].g=g;
                p[idx].b=b;
                p[idx].a=a;
                break;
            }
            case 6:
            {
                //rgb16
                //~ std::assert(fmt == "RGB16");
                RGB16Pixel* p = (RGB16Pixel*)pixels();
                p[idx].r=r<<8;
                p[idx].g=g<<8;
                p[idx].b=b<<8;
                break;
            }
            case 8:
            {
                //rgba16
                //~ std::assert(fmt == "RGBA16");
                RGBA16Pixel* p = (RGBA16Pixel*)pixels();
                p[idx].r=r<<8;
                p[idx].g=g<<8;
                p[idx].b=b<<8;
                p[idx].a=a<<8;
                break;
            }
            default:
                throw std::runtime_error("Bad Bpp:" + std::to_string(Bpp));
        }
    }
    
    std::string format() const {
        return fmt;
    }
    std::uint8_t* pixels(){
        return (uint8_t*)(pixbuff.data());
    }
    const std::uint8_t* pixels() const {
        return (uint8_t*)(pixbuff.data());
    }

    void writePng(std::string fname) const {
        std::vector<std::uint8_t> pix;
        encodePng(w,h,fmt,pixels(),pix);
        std::ofstream out(fname,std::ios::binary);
        out.write((char*)pix.data(),pix.size());
        return;
    }
    void writeBmp(std::string fname) const {
        std::vector<std::uint8_t> pix;
        encodeBmp(w,h,fmt,pixels(),pix);
        std::ofstream out(fname,std::ios::binary);
        out.write((char*)pix.data(),pix.size());
        return;
    }
    
private:
    struct alignas(32) XMM {
        char pix[32];
    };
    unsigned w,h;
    unsigned Bpp;   //bytes per pixel
    std::string fmt;
    std::vector<XMM> pixbuff;
    
    
    void initialize(const void* data, unsigned size){
        std::vector<std::uint8_t> tmp;
        auto ph = pngHeader();
        auto jh = jpgHeader();
        if( compare(ph.data(),ph.size(),data,size) )
            decodePng(data,size, w,h,fmt, this->Bpp,tmp);
        else if( compare(jh.data(),jh.size(),data,size) )
            decodeJpeg(data,size, w,h,fmt,this->Bpp,tmp);
        else
            throw std::runtime_error("Image is neither PNG nor JPEG");
            
        pixbuff.resize(tmp.size()/32+1);
        std::memcpy(pixels(), tmp.data(), tmp.size() );
    }
    
    static 
    std::vector<std::uint8_t> pngHeader(){
        return std::vector<std::uint8_t>( {137,80,78,71,13,10,26,10} );
    }
    static 
    std::vector<std::uint8_t> jpgHeader(){
        return std::vector<std::uint8_t>({0xff, 0xd8, 0xff, 0xe0});
    }
    
	static
    bool compare(const void* l1v, size_t l1size, const void* l2v, size_t l2size){
        const char* l1 = (const char*)l1v;
        const char* l2 = (const char*)l2v;
		size_t le = ((l1size < l2size) ? l1size : l2size);
        if( le == 0 )   
            return false;
        for(std::size_t i=0;i<le;++i){
            if(l1[i] != l2[i])
                return false;
        }
        return true;
    }

    #ifdef WIN32
    
    static
    IWICImagingFactory* getFactory(){
        static IWICImagingFactory* factory = nullptr;
        if( factory == nullptr ){
            auto res = CoInitialize(nullptr);
            if( res != S_FALSE && res != S_OK ){
                throw std::runtime_error("Could not CoInitialize OLE32");
            }
            auto hr = CoCreateInstance(
                CLSID_WICImagingFactory,
                nullptr,
                CLSCTX_INPROC_SERVER,
                IID_IWICImagingFactory,
                (LPVOID*)&factory);
		if (hr != S_FALSE && hr != S_OK)
				throw std::runtime_error("Could not create WICImagingFactory");
        }
        return factory;
    }

    template<typename T>
    class Releaser{
    public:
        T* ptr;
        Releaser(T* x) : ptr(x) {}
        ~Releaser(){ ptr->Release(); }
    };

    static
    void encodePng(unsigned w, unsigned h, std::string fmt, const void* data, 
        std::vector<std::uint8_t>& pngData){
            
        auto factory = getFactory();
        
        IStream* stream;
        auto hr = CreateStreamOnHGlobal(nullptr, TRUE, &stream);
        if( hr!=0 )
            throw std::runtime_error("Could not create stream");
        Releaser<IStream> streamReleaser(stream);
        
        IWICBitmapEncoder* enc;
        hr = factory->CreateEncoder( GUID_ContainerFormatPng , nullptr, &enc );
        if( hr!=0 )
            throw std::runtime_error("Could not create stream");
        Releaser<IWICBitmapEncoder> encoderReleaser(enc);
        //2=no cache
        hr = enc->Initialize(stream, WICBitmapEncoderNoCache);	//nocache = 2
        if( hr!=0 )
            throw std::runtime_error("Could not initialize encoder");

        IWICBitmapFrameEncode* frame;
        
        //MS example doesn't release the property bag...
        IPropertyBag2* props;
        
        hr = enc->CreateNewFrame(&frame,&props);
        if( hr!=0 )
            throw std::runtime_error("Could not create frame encoder");
        Releaser<IWICBitmapFrameEncode> frameReleaser(frame);
        
        hr = frame->Initialize(props);
        if( hr!=0 )
            throw std::runtime_error("Could not initialize frame");

        hr = frame->SetSize(w,h);
        if( hr!=0 )
            throw std::runtime_error("Could not set size");
        
        WICPixelFormatGUID pf;
		unsigned stride;

        if( fmt == "RGB8" ) {
            stride = w*3;
            pf = GUID_WICPixelFormat24bppRGB;
        } else if( fmt == "RGBA8" ){
            pf = GUID_WICPixelFormat32bppRGBA;
            stride = w*4;
        } else if( fmt == "RGB16" ){
            pf = GUID_WICPixelFormat48bppRGB;
            stride = w*6;
        } else if( fmt == "RGBA16" ){
            pf = GUID_WICPixelFormat64bppRGBA;
            stride = w*8;
        } else {
            throw std::runtime_error("Unknown pixel format");
        }
        
		IWICBitmap* bmp;
		hr = factory->CreateBitmapFromMemory(w, h, pf, stride, h*stride, (BYTE*)data, &bmp);
		if (hr != 0)
			throw std::runtime_error("Cannot create bitmap");
		Releaser<IWICBitmap> bmpReleaser(bmp);

        hr = frame->SetPixelFormat(&pf);
        if( hr!=0 )
            throw std::runtime_error("Could not set pixel format");
        
		IWICFormatConverter *conv;
		hr = factory->CreateFormatConverter(&conv);
		if (hr != 0)
			throw std::runtime_error("Cannot create format converter");
		Releaser<IWICFormatConverter> convR(conv);

		hr = conv->Initialize(bmp, pf, WICBitmapDitherTypeNone, nullptr, 0.0f, WICBitmapPaletteTypeCustom);
		if (hr != 0)
			throw std::runtime_error("Cannot initialize format converter");
 
		hr = frame->WriteSource(bmp, nullptr);
        if( hr!=0 )
            throw std::runtime_error("Could not write pixels");
        
        hr = frame->Commit();
        if( hr!=0 )
            throw std::runtime_error("Could not commit frame");
            
        hr = enc->Commit();
        if( hr!=0 )
            throw std::runtime_error("Could not commit encoder");

        ULARGE_INTEGER pos;
		LARGE_INTEGER zero;
		zero.QuadPart = 0;
        hr = stream->Seek(zero,STREAM_SEEK_CUR,&pos);
        if( hr!=0 )
            throw std::runtime_error("Could not seek 0");
        
        pngData.resize( (unsigned) pos.QuadPart );

        hr = stream->Seek(zero,STREAM_SEEK_SET,nullptr);
        if( hr!=0 )
            throw std::runtime_error("Could not seek to start");
            
        unsigned numLeft = (unsigned)pngData.size();
        std::uint8_t* p = pngData.data();
        while(numLeft > 0 ){
            ULONG numRead;
            hr = stream->Read( p,numLeft,&numRead);
            if( hr!=0 )
                throw std::runtime_error("Could not read");
            numLeft -= numRead;
            p += numRead;
        }
    }

    static
    void decodePng(const void* data, unsigned dataSize, unsigned& w, unsigned& h, 
            std::string& fmt, unsigned& Bpp, std::vector<std::uint8_t>& pix){
        decodeAny(data,dataSize,w,h,Bpp,fmt,pix);
    }
    static     
    void decodeJpeg(const void* data, unsigned dataSize, unsigned& w, unsigned& h, 
        std::string& fmt, unsigned& Bpp,std::vector<std::uint8_t>& ddata){
        decodeAny(data,dataSize,w,h,Bpp,fmt,ddata);
    }
    static     
    void decodeAny(const void* indata, unsigned dataSize, unsigned& w, unsigned& h,
        unsigned& Bpp,
        std::string& fmt, std::vector<std::uint8_t>& returnedImage)
    {
        {
			const char* indatac = (const char*)indata;
            std::vector<std::uint8_t> tmp;
            for(unsigned i=0;i<8 && i<dataSize;++i){
                tmp.push_back(indatac[i]);
            }
			auto ph = pngHeader();
			auto jh = jpgHeader();
            if( !compare(ph.data(),ph.size(), tmp.data(), tmp.size()) && 
				!compare(jh.data(),jh.size(), tmp.data(),tmp.size()))
                throw std::runtime_error("Image is neither PNG nor JPEG");
        }
        
        auto factory = getFactory();
        IWICStream* stream;
        HRESULT hr = factory->CreateStream(&stream);
        if(hr!=0)
            throw std::runtime_error("Could not create IWICStream");
        Releaser<IWICStream> sr(stream);
		stream->InitializeFromMemory((BYTE*)indata, dataSize);
        if(hr!=0)
            throw std::runtime_error("Could not initialize stream from memory");

        IWICBitmapDecoder* dec;
        hr = factory->CreateDecoderFromStream(stream,nullptr,WICDecodeMetadataCacheOnLoad,&dec);	//cache on load=1
        if(hr!=0)
            throw std::runtime_error("Could not create decoder from stream");
        Releaser<IWICBitmapDecoder> dr(dec);
        
        IWICBitmapFrameDecode* bmp;
        hr = dec->GetFrame(0,&bmp);
        if(hr!=0)
            throw std::runtime_error("Could not get frame from decoder");
        Releaser<IWICBitmapFrameDecode> br(bmp);
        
        UINT ww,hh;
        bmp->GetSize(&ww,&hh);
        w=(unsigned)ww;
        h=(unsigned)hh;

        WICPixelFormatGUID pfmt;
        hr = bmp->GetPixelFormat(&pfmt);
        if(hr!=0)
            throw std::runtime_error("Could not get pixel format");
        WICPixelFormatGUID convertFmt;
        
		if(memcmp( &GUID_WICPixelFormat64bppRGBA, &pfmt, sizeof(pfmt) ) == 0 ||
            memcmp( &GUID_WICPixelFormat64bppBGRA, &pfmt, sizeof(pfmt) ) == 0 ){
            convertFmt = GUID_WICPixelFormat64bppRGBA;
            fmt="RGBA16";
            Bpp = 8;
        }
		else{
			convertFmt = GUID_WICPixelFormat32bppRGBA;
			fmt = "RGBA8";
			Bpp = 4;
		}

		IWICFormatConverter* conv;
		hr = factory->CreateFormatConverter(&conv);
		if (hr != 0)
			throw std::runtime_error("Cannot create converter");
		Releaser<IWICFormatConverter> convR(conv);

		hr = conv->Initialize(bmp, convertFmt, WICBitmapDitherTypeNone, nullptr, 0.0f, WICBitmapPaletteTypeCustom);
		if (hr != 0)
			throw std::runtime_error("Cannot initialize converter");

		returnedImage.resize(w*h*Bpp);
        hr = conv->CopyPixels(nullptr,w*Bpp,w*h*Bpp,returnedImage.data());
        if(hr!=0)
            throw std::runtime_error("Could not copy pixels");
        
        return;
    }
    
    #else  //not WIN32

    struct JpegReleaser{
        tjhandle h;
        JpegReleaser(tjhandle hh) : h(hh) {}
        ~JpegReleaser(){
            tjDestroy(h);
        }
    };
        
    static     
    void decodeJpeg(const void* datav, unsigned dataSize, unsigned& w, unsigned& h, 
        std::string& fmt, unsigned& Bpp, std::vector<std::uint8_t>& pix){
    
        const uint8_t* data = (const uint8_t*) datav;
        tjhandle handle = tjInitDecompress();
        JpegReleaser r(handle);
        int ww,hh,subsamp,colorspace;
        int rv = tjDecompressHeader3( handle , data, dataSize,
            &ww, &hh, &subsamp, &colorspace );
        if(rv != 0)
            throw std::runtime_error("Cannot decompress JPEG header");
                
        w = (unsigned)ww;
        h = (unsigned)hh;
        fmt = "RGBA8";
        Bpp=4;
        
        pix.resize(w*h*4);
            
        rv = tjDecompress2( handle, data, dataSize,
            pix.data(), w,w*4,h, TJPF_RGBA, 0);
        if(rv != 0)
            throw std::runtime_error("Cannot decompress JPEG");
    }   
    
    
    struct PngFree{
        png_structp ps;
        png_infop ip;
        PngFree(png_structp ps_, png_infop ip_) : ps(ps_), ip(ip_) {}
        ~PngFree(){ 
            png_destroy_read_struct(&ps, &ip, nullptr);
        }
    };

    struct IORData{
        const void* data;
        unsigned offset;
        IORData(const void* d) : data(d), offset(0){}
    };
    
    static
    void loadCallback(png_structp ps, png_bytep data, png_size_t len){
        IORData* dat = (IORData*) png_get_io_ptr(ps);
        const uint8_t* p = (const uint8_t*)dat->data;
        memcpy(data, p+dat->offset, len );
        dat->offset += len;
    }

    struct IOWData{
        std::vector<std::uint8_t>& data;
        IOWData(std::vector<std::uint8_t>& d) : data(d){}
    };

    static
    void saveCallback(png_structp ps, png_bytep data, png_size_t len){
        IOWData* dat = (IOWData*) png_get_io_ptr(ps);
        while(len>0){
            dat->data.push_back(*data);
            data++;
            len--;
        }
    }

    static
    void flushCallback(png_structp ps){
    }
        
    static
    void decodePng(const void* data, unsigned dataSize, unsigned& w, unsigned& h, 
            std::string& fmt, unsigned& Bpp, std::vector<std::uint8_t>& pix){
        png_structp ps = png_create_read_struct(png_get_libpng_ver(0),0,0,0);
        png_infop ip = png_create_info_struct(ps);
        if(!ip ){
            png_destroy_read_struct(&ps, (ip? &ip : NULL), NULL );
            throw std::runtime_error("No PNG info struct");
        }
        
        PngFree pf(ps,ip);
        
        IORData idata(data);
        png_set_read_fn(ps, &idata, Image::loadCallback );
        
        //this seems to have no effect...
        //png_set_swap(ps);
        
        png_read_info(ps,ip);

        png_set_tRNS_to_alpha(ps);    
        png_set_gray_to_rgb(ps);
        png_set_expand_gray_1_2_4_to_8(ps);
        png_set_palette_to_rgb(ps);
        png_set_expand(ps);
        png_set_add_alpha(ps,0xffff,1);
        
        //png_set_bgr(ps)
        
        png_read_update_info(ps,ip);

        w = (unsigned) png_get_image_width(ps,ip);
        h = (unsigned) png_get_image_height(ps,ip);

        //auto colorType = png_get_color_type(ps,ip);
        auto bitDepth = png_get_bit_depth(ps,ip);

        Bpp = 4*bitDepth/8;  //bytes per pixel


        pix.resize(w*h*Bpp);
        
        if( bitDepth == 8 )
            fmt = "RGBA8";
        else if( bitDepth == 16 )
            fmt = "RGBA16";
        else
            throw std::runtime_error("Unexpected bpp:"+std::to_string(bitDepth));
            
        uint8_t* p = pix.data();    
        for(unsigned i=0;i<h;++i){
            png_read_row(ps,p,nullptr);
            p += w*Bpp;
        }
        //byte swap, since libpng refuses to do it
        if( bitDepth == 16 ){
            for(unsigned j=0;j<pix.size();j+=2){
                std::swap(pix[j],pix[j+1]);
            }
        }
            
    }

    static
    void encodePng(unsigned w, unsigned h, std::string fmt, const void* data, 
        std::vector<std::uint8_t>& pngData){
        png_structp ps = png_create_write_struct(png_get_libpng_ver(0),0,0,0);
        png_infop ip = png_create_info_struct(ps);
        if(!ip ){
            png_destroy_read_struct(&ps, (ip? &ip : NULL), NULL );
            throw std::runtime_error("No PNG info struct");
        }
        
        PngFree pf(ps,ip);

        pngData.clear();
        IOWData idata(pngData);
        png_set_write_fn(ps, &idata, Image::saveCallback, Image::flushCallback );

        unsigned f,incr,bpp;
        if( fmt == "RGB8" ){
            f = PNG_COLOR_TYPE_RGB;
            incr = 3*w;
            bpp=8;
        } else if( fmt == "RGBA8" ){
            f = PNG_COLOR_TYPE_RGB_ALPHA;
            incr = 4*w;
            bpp=8;
        } else if( fmt == "RGB16"){
            f = PNG_COLOR_TYPE_RGB;
            incr = 6*w;
            bpp=16;
        } else if( fmt == "RGBA16"){
            f = PNG_COLOR_TYPE_RGB_ALPHA;
            incr = 8*w;
            bpp=16;
        } else{
            throw std::runtime_error("Unknown format");
        }
        
        png_set_IHDR( ps, ip, w,h, bpp, f, PNG_INTERLACE_NONE, 
            PNG_COMPRESSION_TYPE_DEFAULT, PNG_FILTER_TYPE_DEFAULT);
        png_write_info( ps, ip );
        //png_set_bgr(ps);

        if( bpp == 16 ){
            std::uint8_t* p = (std::uint8_t*) data;
            std::vector<std::uint8_t> swabby(incr);
            for(unsigned y=0;y<h;++y){
                memcpy(swabby.data(),p,incr);
                for(unsigned i=0;i<incr;i+=2){
                    std::swap(swabby[i],swabby[i+1]);
                }
                png_write_row(ps,swabby.data());
                p += incr;
            }
        } else {
            std::uint8_t* p = (std::uint8_t*) data;
            for(unsigned y=0;y<h;++y){
                png_write_row(ps,p);
                p += incr;
            }
        }
        
        png_write_end(ps,ip);
        
    }
	#endif
    
    
    #pragma pack(push,1)
    struct BitmapHeader{
        std::uint16_t sig;       //BM
        std::uint32_t size;      //whole file
        std::uint32_t reserved;  
        std::uint32_t offset;    //size of header
        std::uint32_t header_size;   //rest of header
        std::uint32_t width,height;
        std::uint16_t planes;    //always 1
        std::uint16_t bpp;       //24=true color
        std::uint32_t compression;   //0=none
        std::uint32_t img_size;  //bytes in image
        std::uint32_t ppm_x,ppm_y;   //pixels per meter
        std::uint32_t ncolors;       //num colors
        std::uint32_t icolors;       //important colors
    };
    #pragma pack(pop)
    
    static
    void encodeBmp(unsigned w, unsigned h, std::string fmt, const void* data, 
        std::vector<std::uint8_t>& bmpData){
        
        auto rowsize = w*3;
        unsigned padding=0;
        if( rowsize % 4 != 0 )
            padding = 4-(rowsize%4);
            
        auto pitch = rowsize + padding;
        BitmapHeader hdr;
        hdr.sig = 0x4d42;
        hdr.size = pitch*h+sizeof(BitmapHeader);
        hdr.reserved = 0;
        hdr.offset = sizeof(BitmapHeader);
        hdr.header_size = 40; 
        hdr.width = w;
        hdr.height = h;
        hdr.planes = 1;
        hdr.bpp = 24;
        hdr.compression = 0;
        hdr.img_size = 3*w*h;
        hdr.ppm_x = 2834;
        hdr.ppm_y = 2834;
        hdr.ncolors = 0;
        hdr.icolors = 0;
        
        bmpData.resize(sizeof(hdr));
        memcpy(bmpData.data(), &hdr, sizeof(hdr));

        unsigned i=0;
        unsigned totalPix = w*h;
        std::uint8_t* p8 = (std::uint8_t*) data;
        std::uint16_t* p16 = (std::uint16_t*) data;
        while(i < totalPix ){
            for(unsigned x=0;x<w;++x){
                if( fmt == "RGB8" ){
                    bmpData.push_back(p8[2]);
                    bmpData.push_back(p8[1]);
                    bmpData.push_back(p8[0]);
                    p8+=3;
                //~ } else if( fmt == "BGR8" ){
                    //~ bmpData.push_back(p8[0]);
                    //~ bmpData.push_back(p8[1]);
                    //~ bmpData.push_back(p8[2]);
                    //~ p8+=3;
                //~ } else if( fmt == "BGRA8" ){
                    //~ bmpData.push_back(p8[0]);
                    //~ bmpData.push_back(p8[1]);
                    //~ bmpData.push_back(p8[2]);
                    //~ p8+=4;
                } else if( fmt == "RGBA8" ){
                    bmpData.push_back(p8[2]);
                    bmpData.push_back(p8[1]);
                    bmpData.push_back(p8[0]);
                    p8+=4;
                } else if( fmt == "RGB16" ){
                    bmpData.push_back(p16[2]>>8);
                    bmpData.push_back(p16[1]>>8);
                    bmpData.push_back(p16[0]>>8);
                    p16+=3;
                } else if( fmt == "RGBA16" ){
                    bmpData.push_back(p16[2]>>8);
                    bmpData.push_back(p16[1]>>8);
                    bmpData.push_back(p16[0]>>8);
                    p16+=4;
                } else{
                    throw std::runtime_error("Bad format");
                }
                i++;
            }
            if( padding != 0 )
                bmpData.resize(bmpData.size()+padding);
        }
    }
};
