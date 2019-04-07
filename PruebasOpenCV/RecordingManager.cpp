#include "stdafx.h"
#include "CamerasManager.h"
#include "RecordingManager.h"
#include "VideoWriter.h"
#include "AForgeVideoWriter.h"

#include <thread>
#include <stdio.h>
#include <iostream>

#include "opencv2/highgui/highgui.hpp"
#include "opencv2/imgproc/imgproc.hpp"

// One capture variable for each stream
cv::VideoCapture cap1;
cv::VideoCapture cap2;

// Variable to know which stream is currently active
bool streamOne;

// This will end the recording thread loop
bool recording;

// Some bool variable to control recording states with threads
bool changeCamera;
bool changeStream;
bool stopCamera;

// Save which camera is currently recording
int currentCamera;

// Save which stream is using which camera
int stream1Camera;
int stream2Camera;

// Frame size
double width;
double height;

CamerasManager camerasManager;
//VideoWriter videoWriter;
AForgeVideoWriter videoWriter;

std::thread recordingThread;

RecordingManager::RecordingManager(void)
{
	camerasManager = CamerasManager();
}


RecordingManager::~RecordingManager(void)
{
}

void CameraActivation()
{
	try
	{
		currentCamera = camerasManager.ChangeCamera();

		if(streamOne)
		{
			cap2 = cv::VideoCapture(currentCamera);
		}
		else
		{
			cap1 = cv::VideoCapture(currentCamera);
		}
		std::this_thread::sleep_for (std::chrono::seconds(1));
		changeCamera = false;
		changeStream = true;
	}
	catch(int e)
	{
		std::cout << "An exception occurred while activating a camera. Exception Nr. " << e << '\n';
	}
}

void CameraStopping()
{
	try
	{
		if(streamOne)
		{
			cap2.release();
		}
		else
		{
			cap1.release();
		}
		stopCamera = false;
		changeCamera = true;
	}
	catch(int e)
	{
		std::cout << "An exception occurred while stopping a camera. Exception Nr. " << e << '\n';
	}
}

void RecordingThread()
{
	int timer = rand() % 200 + 100;
	int counter = 0;

	int falseTimestamp = 0;

	changeCamera = true;
	changeStream = false;
	stopCamera = false;

	while (recording)
    {
		counter++;

		if(counter == timer)
		{
			counter = 0;

			// If it's a camera change, we prepare the next one
			if(changeCamera)
			{				
				timer = rand() % 200 + 300; // timer for long time (3-5 seconds)

				// Start camera activation thread	
				std::thread(CameraActivation).detach();
			}	
			// The programm should never enter here except if the previous stopping failed
			else if(stopCamera)
			{
				timer = rand() % 200 + 300; // timer for long time (3-5 seconds)

				// Start camera stopping thread
				std::thread(CameraStopping).detach();
			}
		}

		if(changeStream)
		{
			changeStream = false;
			streamOne = !streamOne;
			stopCamera = true;

			// Launch camera stopping thread
			timer = rand() % 200 + 300; // timer for long time (3-5 seconds)
			std::thread(CameraStopping).detach();
		}

		try
		{
			cv::Mat frame;

			bool bSuccess;

			if(streamOne)
				bSuccess = cap1.read(frame); // read a new frame from video
			else
				bSuccess = cap2.read(frame);

			if (!bSuccess) //if not success, show info
			{
				 std::cout << "Cannot read a frame from video stream" << std::endl;
			}
			else
			{
				imshow("MyVideo", frame); //show the frame in "MyVideo" window
				cv::Mat frameCopy;
				cv::cvtColor(frame, frameCopy, CV_BGR2RGB);
				videoWriter.WriteFrame(frameCopy, falseTimestamp);
				frameCopy.release();
			}
		}
		catch(int e)
		{

		}
		std::this_thread::sleep_for (std::chrono::milliseconds(1));


		falseTimestamp++;
    }

	// When finishing, wait for sevral seconds and release all cameras, because we don't know if there is
	// any thread starting one
	std::this_thread::sleep_for (std::chrono::seconds(5));
	if(cap1.isOpened())
		cap1.release();
	if(cap2.isOpened())
		cap2.release();
	videoWriter.Close();
}

int RecordingManager::StartRecording()
{
	recording = true;
	streamOne = true;
	int result;

	// Create video file
	result = videoWriter.Open("prueba.avi", 640, 480);
	if (result)
	{
		std::cout << "Error opening the video file \\n";
		return -1;
	}

	// Start stream one
	stream1Camera = camerasManager.ChangeCamera();
	try
	{
		cap1 = cv::VideoCapture(stream1Camera);
	}
	catch(int e)
	{
		std::cout << "An exception occurred trying to start the first camera. Exception Nr. " << e << '\n';
		return -1;
	}

	cv::namedWindow("MyVideo",CV_WINDOW_AUTOSIZE); //create a window called "MyVideo"

	// Start recording thread
	recordingThread = std::thread(RecordingThread);
}

void RecordingManager::StopRecording()
{
	// Stop the recording thread and wait until it ends
	recording = false;
	recordingThread.join();
}




