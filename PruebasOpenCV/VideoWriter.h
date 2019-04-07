#pragma once

extern "C"
{
	#include <libavutil/opt.h>
	#include <libavutil/channel_layout.h>
	#include <libavutil/common.h>
	#include <libavutil/imgutils.h>
	#include <libavutil/mathematics.h>
	#include <libavutil/samplefmt.h>
	#include <libswscale/swscale.h>
	#include <libavcodec/avcodec.h>
	#include <libavformat/avformat.h>
}

#include "opencv2/highgui/highgui.hpp"

class VideoWriter
{
public:
	VideoWriter();
	~VideoWriter();
	int Open(const char* filename, int width, int height);
	int WriteFrame(cv::Mat CVframe, double timestamp);
	int Close();
};

