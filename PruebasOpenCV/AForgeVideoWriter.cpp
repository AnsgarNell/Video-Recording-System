#include "stdafx.h"
#include "AForgeVideoWriter.h"

namespace libffmpeg
{
	extern "C"
	{
		// disable warnings about badly formed documentation from FFmpeg, which don't need at all
		#pragma warning(disable:4635) 
		// disable warning about conversion int64 to int32
		#pragma warning(disable:4244) 

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
}

// A structure to encapsulate all FFMPEG related private variable
struct WriterPrivateData
{
public:
	libffmpeg::AVFormatContext*		FormatContext;
	libffmpeg::AVStream*			VideoStream;
	libffmpeg::AVFrame*				VideoFrame;
	struct libffmpeg::SwsContext*	ConvertContext;
	struct libffmpeg::SwsContext*	ConvertContextGrayscale;

	libffmpeg::uint8_t*	VideoOutputBuffer;
	int VideoOutputBufferSize;

	WriterPrivateData()
	{
		FormatContext = NULL;
		VideoStream = NULL;
		VideoFrame = NULL;
		ConvertContext = NULL;
		ConvertContextGrayscale = NULL;
		VideoOutputBuffer = NULL;
	}
};

const int frameRate = 25;
const int bitRate = 350000;

WriterPrivateData *data;
libffmpeg::SwsContext *rgb_to_yuv_context;
libffmpeg::AVCodecContext *c = NULL;

static void write_video_frame(WriterPrivateData *data);
static void open_video(WriterPrivateData *data);
static void add_video_stream(WriterPrivateData *data, int width, int height, int frameRate, int bitRate,
enum libffmpeg::AVCodecID codec_id, enum libffmpeg::PixelFormat pixelFormat);

AForgeVideoWriter::AForgeVideoWriter()
{
	data = nullptr;
	libffmpeg::av_register_all();
}


AForgeVideoWriter::~AForgeVideoWriter()
{
}

int AForgeVideoWriter::Open(const char* filename, int width, int height)
{
	data = new WriterPrivateData;
	int ret;

	try
	{
		/* allocate the output media context */
		avformat_alloc_output_context2(&(data->FormatContext), NULL, NULL, filename);
		if (!data->FormatContext) 
		{
			fprintf(stderr, "Could not deduce output format from file extension: using MPEG.\n");
			avformat_alloc_output_context2(&(data->FormatContext), NULL, "mpeg", filename);
		}
		if (!data->FormatContext)
		{
			fprintf(stderr, "Error occurred when getting output format\n");
			exit(1);
		}

		// add video stream using the specified video codec
		add_video_stream(data, width, height, frameRate, bitRate, data->FormatContext->oformat->video_codec,
			libffmpeg::PIX_FMT_YUV420P);

		open_video(data);

		// open output file
		if (!(data->FormatContext->oformat->flags & AVFMT_NOFILE))
		{
			if (libffmpeg::avio_open(&data->FormatContext->pb, filename, AVIO_FLAG_WRITE) < 0)
			{
				fprintf(stderr, "Cannot open the video file.\n");
				exit(1);
			}
		}

		/* Write the stream header, if any. */
		ret = avformat_write_header(data->FormatContext, NULL);
		if (ret < 0) 
		{
			fprintf(stderr, "Error occurred when opening output file: %s\n", filename);
			exit(1);
		}

		libffmpeg::AVCodec *codec;
		libffmpeg::AVCodecID codec_id = libffmpeg::CODEC_ID_MPEG1VIDEO;

		libffmpeg::avcodec_register_all();

		/* find the mpeg1 video encoder */
		codec = avcodec_find_encoder(codec_id);
		if (!codec) {
			fprintf(stderr, "Codec not found\n");
			exit(1);
		}

		c = avcodec_alloc_context3(codec);
		if (!c) {
			fprintf(stderr, "Could not allocate video codec context\n");
			exit(1);
		}

		/* resolution must be a multiple of two */
		c->width = width;
		c->height = height;
		c->pix_fmt = libffmpeg::AV_PIX_FMT_YUV420P;

		rgb_to_yuv_context = libffmpeg::sws_getContext(c->width, c->height, libffmpeg::PIX_FMT_RGB24,
			c->width, c->height, c->pix_fmt, SWS_BICUBIC, NULL, NULL, NULL);
	}
	catch (int e)
	{

	}

	return 0;
}

int AForgeVideoWriter::Close()
{
	if (data != nullptr)
	{
		if (data->FormatContext)
		{
			if (data->FormatContext->pb != NULL)
			{
				libffmpeg::av_write_trailer(data->FormatContext);
			}

			if (data->VideoStream)
			{
				libffmpeg::avcodec_close(data->VideoStream->codec);
			}

			if (data->VideoFrame)
			{
				libffmpeg::av_free(data->VideoFrame->data[0]);
				libffmpeg::av_free(data->VideoFrame);
			}

			if (data->VideoOutputBuffer)
			{
				libffmpeg::av_free(data->VideoOutputBuffer);
			}

			for (unsigned int i = 0; i < data->FormatContext->nb_streams; i++)
			{
				libffmpeg::av_freep(&data->FormatContext->streams[i]->codec);
				libffmpeg::av_freep(&data->FormatContext->streams[i]);
			}

			if (data->FormatContext->pb != NULL)
			{
				libffmpeg::avio_close(data->FormatContext->pb);
			}

			libffmpeg::av_free(data->FormatContext);
		}

		if (data->ConvertContext != NULL)
		{
			libffmpeg::sws_freeContext(data->ConvertContext);
		}

		if (data->ConvertContextGrayscale != NULL)
		{
			libffmpeg::sws_freeContext(data->ConvertContextGrayscale);
		}

		data = nullptr;
	}

	return 0;
}

libffmpeg::AVFrame *CVMatToAVFrame(cv::Mat cvFrame)
{
	libffmpeg::AVFrame *avFrame;
	libffmpeg::AVFrame *avFrameRescaledFrame;
	int ret;

	avFrame = libffmpeg::av_frame_alloc();
	if (!avFrame) {
		fprintf(stderr, "Could not allocate video frame\n");
		exit(1);
	}
	avFrame->height = c->height;
	avFrame->width = c->width;
	avFrame->format = libffmpeg::AV_PIX_FMT_BGR24;
	//avpicture_alloc((AVPicture*)avFrame, AV_PIX_FMT_RGB24, c->width, c->height);

	// the image can be allocated by any means and av_image_alloc() is
	// just the most convenient way if av_malloc() is to be used
	ret = libffmpeg::av_image_alloc(avFrame->data, avFrame->linesize, c->width, c->height,
		libffmpeg::AV_PIX_FMT_BGR24, 32);
	if (ret < 0) {
		fprintf(stderr, "Could not allocate raw picture buffer\n");
		exit(1);
	}

	avFrameRescaledFrame = libffmpeg::av_frame_alloc();
	if (!avFrameRescaledFrame) {
		fprintf(stderr, "Could not allocate video frame\n");
		exit(1);
	}
	avFrameRescaledFrame->height = c->height;
	avFrameRescaledFrame->width = c->width;
	avFrameRescaledFrame->format = c->pix_fmt;
	//ret = avpicture_alloc((AVPicture*)avFrameRescaledFrame, AV_PIX_FMT_RGB24, c->width, c->height);

	// the image can be allocated by any means and av_image_alloc() is
	// just the most convenient way if av_malloc() is to be used
	ret = libffmpeg::av_image_alloc(avFrameRescaledFrame->data, avFrameRescaledFrame->linesize, c->width, c->height,
		c->pix_fmt, 32);
	if (ret < 0) {
		fprintf(stderr, "Could not allocate raw picture buffer\n");
		exit(1);
	}

	for (int h = 0; h < c->height; h++)
	{
		memcpy(&(avFrame->data[0][h*avFrame->linesize[0]]), &(cvFrame.data[h*cvFrame.step]), c->width * 3);
	}

	// rescale to outStream format
	libffmpeg::sws_scale(rgb_to_yuv_context, avFrame->data, avFrame->linesize, 0, c->height, avFrameRescaledFrame->data, avFrameRescaledFrame->linesize);

	libffmpeg::av_freep(&avFrame->data[0]);
	av_frame_free(&avFrame);

	return avFrameRescaledFrame;
}

int AForgeVideoWriter::WriteFrame(cv::Mat CVframe, double timestamp)
{
	// Convert CV Mat to AVFrame and store it in data
	data->VideoFrame = CVMatToAVFrame(CVframe);
	
	/*
	if (timestamp.Ticks >= 0)
	{
		const double frameNumber = timestamp.TotalSeconds * m_frameRate;
		data->VideoFrame->pts = static_cast<libffmpeg::int64_t>(frameNumber);
	}
	*/
	//BORRAR
	data->VideoFrame->pts = timestamp;

	// write the converted frame to the video file
	write_video_frame(data);

	libffmpeg::av_freep(&data->VideoFrame->data[0]);
	av_frame_free(&data->VideoFrame);

	return 0;
}

// Writes video frame to opened video file
void write_video_frame(WriterPrivateData *data)
{
	libffmpeg::AVCodecContext* codecContext = data->VideoStream->codec;
	int out_size, ret = 0;

	if (data->FormatContext->oformat->flags & AVFMT_RAWPICTURE)
	{
		fprintf(stderr, "raw picture must be written\n");
	}
	else
	{
		// encode the image
		out_size = libffmpeg::avcodec_encode_video(codecContext, data->VideoOutputBuffer,
			data->VideoOutputBufferSize, data->VideoFrame);

		// if zero size, it means the image was buffered
		if (out_size > 0)
		{
			libffmpeg::AVPacket packet;
			libffmpeg::av_init_packet(&packet);

			//if (codecContext->coded_frame->pts != AV_NOPTS_VALUE)
			//{
				packet.pts = libffmpeg::av_rescale_q(codecContext->coded_frame->pts, codecContext->time_base, data->VideoStream->time_base);
			//}

			if (codecContext->coded_frame->key_frame)
			{
				packet.flags |= AV_PKT_FLAG_KEY;
			}

			packet.stream_index = data->VideoStream->index;
			packet.data = data->VideoOutputBuffer;
			packet.size = out_size;

			// write the compressed frame to the media file
			ret = libffmpeg::av_interleaved_write_frame(data->FormatContext, &packet);
		}
		else
		{
			// image was buffered
		}
	}

	if (ret != 0)
	{
		fprintf(stderr, "Error while writing video frame\n");
		exit(1);
	}
}

// Allocate picture of the specified format and size
static libffmpeg::AVFrame* alloc_picture(enum libffmpeg::PixelFormat pix_fmt, int width, int height)
{
	libffmpeg::AVFrame* picture;
	void* picture_buf;
	int size;

	picture = libffmpeg::avcodec_alloc_frame();
	if (!picture)
	{
		return NULL;
	}

	size = libffmpeg::avpicture_get_size(pix_fmt, width, height);
	picture_buf = libffmpeg::av_malloc(size);
	if (!picture_buf)
	{
		libffmpeg::av_free(picture);
		return NULL;
	}

	libffmpeg::avpicture_fill((libffmpeg::AVPicture *) picture, (libffmpeg::uint8_t *) picture_buf, pix_fmt, width, height);

	return picture;
}

// Create new video stream and configure it
void add_video_stream(WriterPrivateData *data, int width, int height, int frameRate, int bitRate,
enum libffmpeg::AVCodecID codecId, enum libffmpeg::PixelFormat pixelFormat)
{
	libffmpeg::AVCodecContext* codecContex;

	// create new stream
	data->VideoStream = libffmpeg::avformat_new_stream(data->FormatContext, 0);
	if (!data->VideoStream)
	{
		fprintf(stderr, "Failed creating new video stream\n");
		exit(1);
	}

	codecContex = data->VideoStream->codec;
	codecContex->codec_id = codecId;
	codecContex->codec_type = libffmpeg::AVMEDIA_TYPE_VIDEO;

	// put sample parameters
	codecContex->bit_rate = bitRate;
	codecContex->width = width;
	codecContex->height = height;

	// time base: this is the fundamental unit of time (in seconds) in terms
	// of which frame timestamps are represented. for fixed-fps content,
	// timebase should be 1/framerate and timestamp increments should be
	// identically 1.
	codecContex->time_base.den = frameRate;
	codecContex->time_base.num = 1;

	codecContex->gop_size = 12; // emit one intra frame every twelve frames at most
	codecContex->pix_fmt = pixelFormat;

	if (codecContex->codec_id == libffmpeg::CODEC_ID_MPEG1VIDEO)
	{
		// Needed to avoid using macroblocks in which some coeffs overflow.
		// This does not happen with normal video, it just happens here as
		// the motion of the chroma plane does not match the luma plane.
		codecContex->mb_decision = 2;
	}

	// some formats want stream headers to be separate
	if (data->FormatContext->oformat->flags & AVFMT_GLOBALHEADER)
	{
		codecContex->flags |= CODEC_FLAG_GLOBAL_HEADER;
	}
}

// Open video codec and prepare out buffer and picture
void open_video(WriterPrivateData *data)
{
	libffmpeg::AVCodecContext* codecContext = data->VideoStream->codec;
	libffmpeg::AVCodec* codec = avcodec_find_encoder(codecContext->codec_id);

	if (!codec)
	{
		fprintf(stderr, "Cannot find video codec\n");
		exit(1);
	}

	// open the codec 
	if (avcodec_open2(codecContext, codec, NULL) < 0)
	{
		fprintf(stderr, "Cannot open video codec\n");
		exit(1);
	}

	data->VideoOutputBuffer = NULL;
	if (!(data->FormatContext->oformat->flags & AVFMT_RAWPICTURE))
	{
		// allocate output buffer 
		data->VideoOutputBufferSize = 6 * codecContext->width * codecContext->height; // more than enough even for raw video
		data->VideoOutputBuffer = (libffmpeg::uint8_t*) libffmpeg::av_malloc(data->VideoOutputBufferSize);
	}

	// allocate the encoded raw picture
	data->VideoFrame = alloc_picture(codecContext->pix_fmt, codecContext->width, codecContext->height);

	if (!data->VideoFrame)
	{
		fprintf(stderr, "Cannot allocate video picture\n");
		exit(1);
	}

	// prepare scaling context to convert RGB image to video format
	data->ConvertContext = libffmpeg::sws_getContext(codecContext->width, codecContext->height, libffmpeg::PIX_FMT_BGR24,
		codecContext->width, codecContext->height, codecContext->pix_fmt,
		SWS_BICUBIC, NULL, NULL, NULL);
	// prepare scaling context to convert grayscale image to video format
	data->ConvertContextGrayscale = libffmpeg::sws_getContext(codecContext->width, codecContext->height, libffmpeg::PIX_FMT_GRAY8,
		codecContext->width, codecContext->height, codecContext->pix_fmt,
		SWS_BICUBIC, NULL, NULL, NULL);

	if ((data->ConvertContext == NULL) || (data->ConvertContextGrayscale == NULL))
	{
		fprintf(stderr, "Cannot initialize frames conversion context\n");
		exit(1);
	}
}
