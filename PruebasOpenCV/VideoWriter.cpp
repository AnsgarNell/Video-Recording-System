#include "stdafx.h"
#include "VideoWriter.h"

const int FPS = 25;

FILE *f;
AVCodecContext *c = NULL;
SwsContext *rgb_to_yuv_context;

VideoWriter::VideoWriter()
{
}


VideoWriter::~VideoWriter()
{
}

int VideoWriter::Open(const char* filename, int width, int height)
{
	AVCodec *codec;	
	AVCodecID codec_id = CODEC_ID_MPEG1VIDEO;

	avcodec_register_all();

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

	/* put sample parameters */
	c->bit_rate = 400000;
	/* resolution must be a multiple of two */
	c->width = width;
	c->height = height;
	/* frames per second */
	c->time_base.num = 1;
	c->time_base.den = FPS;
	/* emit one intra frame every ten frames
	* check frame pict_type before passing frame
	* to encoder, if frame->pict_type is AV_PICTURE_TYPE_I
	* then gop_size is ignored and the output of encoder
	* will always be I frame irrespective to gop_size
	*/
	c->gop_size = 10;
	c->max_b_frames = 1;
	c->pix_fmt = AV_PIX_FMT_YUV420P;

	if (codec_id == AV_CODEC_ID_H264)
		av_opt_set(c->priv_data, "preset", "slow", 0);

	/* open it */
	if (avcodec_open2(c, codec, NULL) < 0) {
		fprintf(stderr, "Could not open codec\n");
		exit(1);
	}

	f = fopen(filename, "wb");
	if (!f) {
		fprintf(stderr, "Could not open %s\n", filename);
		exit(1);
	}

	rgb_to_yuv_context = sws_getContext(c->width, c->height, PIX_FMT_RGB24, 
		c->width, c->height, c->pix_fmt, SWS_BICUBIC, NULL, NULL, NULL);

	return 0;
}

AVFrame *CVMatToAVFrame(cv::Mat cvFrame)
{
	AVFrame *avFrame;
	AVFrame *avFrameRescaledFrame;
	int ret;

	avFrame = av_frame_alloc();
	if (!avFrame) {
		fprintf(stderr, "Could not allocate video frame\n");
		exit(1);
	}
	avFrame->height = c->height;
	avFrame->width = c->width;
	avFrame->format = AV_PIX_FMT_BGR24;
	//avpicture_alloc((AVPicture*)avFrame, AV_PIX_FMT_RGB24, c->width, c->height);

	// the image can be allocated by any means and av_image_alloc() is
	// just the most convenient way if av_malloc() is to be used
	ret = av_image_alloc(avFrame->data, avFrame->linesize, c->width, c->height,
		AV_PIX_FMT_BGR24, 32);
	if (ret < 0) {
		fprintf(stderr, "Could not allocate raw picture buffer\n");
		exit(1);
	}

	avFrameRescaledFrame = av_frame_alloc();
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
	ret = av_image_alloc(avFrameRescaledFrame->data, avFrameRescaledFrame->linesize, c->width, c->height,
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
	sws_scale(rgb_to_yuv_context, avFrame->data, avFrame->linesize, 0, c->height, avFrameRescaledFrame->data, avFrameRescaledFrame->linesize);

	av_freep(&avFrame->data[0]);
	av_frame_free(&avFrame);

	return avFrameRescaledFrame;
}

int VideoWriter::WriteFrame(cv::Mat CVframe, double timestamp)
{
	AVFrame *frame;
	AVPacket pkt;
	int i, ret, x, y, got_output;

	frame = CVMatToAVFrame(CVframe);

	av_init_packet(&pkt);
	pkt.data = NULL;    // packet data will be allocated by the encoder
	pkt.size = 0;

	frame->pts = timestamp;

	/* encode the image */
	ret = avcodec_encode_video2(c, &pkt, frame, &got_output);
	if (ret < 0) {
		fprintf(stderr, "Error encoding frame\n");
		exit(1);
	}

	if (got_output) {
		fwrite(pkt.data, 1, pkt.size, f);
		av_free_packet(&pkt);
	}

	av_freep(&frame->data[0]);
	av_frame_free(&frame);
}

int VideoWriter::Close()
{
	uint8_t endcode[] = { 0, 0, 1, 0xb7 };

	avcodec_close(c);
	av_free(c);
	/* add sequence end code to have a real mpeg file */
	fwrite(endcode, 1, sizeof(endcode), f);
	return fclose(f);
}
