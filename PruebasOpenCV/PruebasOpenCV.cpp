#include "stdafx.h"
#include "RecordingManager.h"

#include <iostream>
#include <thread>
#include <stdio.h>

#include "opencv2/highgui/highgui.hpp"

int main (int argc, char *argv[])
{
	RecordingManager recordingManager;
	recordingManager.StartRecording();

	while(27 != cv::waitKey(10)); //wait for 'esc' key press for 30ms. If 'esc' key is pressed, break loop
	
	std::cout << "esc key is pressed by user" << std::endl;
	recordingManager.StopRecording();

    return 0;
}




