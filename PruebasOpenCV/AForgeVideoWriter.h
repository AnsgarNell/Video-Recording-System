#pragma once



#include "opencv2/highgui/highgui.hpp"

class AForgeVideoWriter
{
public:
	AForgeVideoWriter();
	~AForgeVideoWriter();
	int Open(const char* filename, int width, int height);
	int WriteFrame(cv::Mat CVframe, double timestamp);
	int Close();
};

