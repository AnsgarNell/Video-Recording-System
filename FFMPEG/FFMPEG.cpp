// FFMPEG.cpp : Defines the entry point for the console application.
//

#define READ 0
#define WRITE 1

#include <string.h>
#include <iostream>
#include <cstdlib>
#include <stdio.h>
#include <sstream>      // std::istringstream
#include <algorithm>    // std::remove
#include <stdlib.h>     /* srand, rand */
#include <time.h>       /* time */
#include <fstream>
#include <sys/types.h>
#include <signal.h>
#include <sys/stat.h>
#include <fstream>
#include <fcntl.h>
#include <chrono>
#include <thread>


// Unfortunately we have to make different calls for different OS
#if defined(WIN32) || defined(_WIN32) || defined(__WIN32) && !defined(__CYGWIN__)
static const char API_name[] = "dshow";
#else
static const char API_name[] = "v4l2";
#endif

static const char recording_file_extension[] = "mp4";
static const char video_file_extension[] = "h264";
static const char audio_file_extension[] = "mp3";

std::string exec_command(char* cmd)
{
	char   psBuffer[128];
	FILE   *pPipe;
	std::string result = "";

	#if defined(WIN32) || defined(_WIN32) || defined(__WIN32) && !defined(__CYGWIN__)
		if ((pPipe = _popen(cmd, "rt")) == NULL)
			exit(1);
	#else
		if ((pPipe = popen(cmd, "r")) == NULL)
			exit(1);
	#endif

	/* Read pipe until end of file, or an error occurs. */
	while (fgets(psBuffer, 128, pPipe))
	{
		result += psBuffer;
	}

	/* Close pipe and print return value of pPipe. */
	if (feof(pPipe))
	{
		#if defined(WIN32) || defined(_WIN32) || defined(__WIN32) && !defined(__CYGWIN__)
			_pclose(pPipe);
		#else
			pclose(pPipe);
		#endif
	}
	return result;
}

int change_camera(int cameraCount, int precCamera)
{
	int nextCamera = 0;
	time_t t;

	/* Intializes random number generator */
	srand((unsigned)time(&t));

	// Return a random camera
	do
	{
		nextCamera = rand() % cameraCount;
	} while (nextCamera == precCamera);

	precCamera = nextCamera;

	return precCamera;
}

void record_audio(char *str)
{
	std::string result = exec_command(str);
}

void record(char *str, int i)
{
	std::string result = exec_command(str);

	// Parse the result to obtain the starting time
	std::istringstream stream(result);
	std::string line;
	std::string start_time;
	std::size_t found;
	std::size_t found_end;

	// Open file to register start times (video)
	str[0] = '\0';
	sprintf(str, "video%i_start.txt", i);
	std::ofstream myfile;
	myfile.open(str);

	while (std::getline(stream, line))
	{
		found = line.find(", start: ");
		if (found != std::string::npos)
		{
			found_end = line.find(", ", found);
			start_time = line.substr(found + 9, found_end - 4);
		}
	}

	myfile.close();
}

int main(int argc, char *argv[])
{
	bool guardar = false;
	std::string devices[10][2];
	char str[1024] = "";
	char audio_file_names[1024] = "";
	char video_file_names[1024] = "";
	int i = 0, j = 0;
	int num_devices = 0;
	std::size_t found = 0;
	int current_camera = 0;
	int video_count = 0;
	int timer = 0;
	int timer_seconds = 0;

	#if defined(WIN32) || defined(_WIN32) || defined(__WIN32) && !defined(__CYGWIN__)
		sprintf(str, "ffmpeg -list_devices true -f %s -i dummy 2>&1", API_name);
	#else
		strcat(str, "v4l2-ctl --list-devices");
	#endif
	
	std::string result = exec_command(str);

	std::istringstream stream(result);
	std::string line;
	while (std::getline(stream, line)) 
	{
		if (guardar)
		{
			guardar = false;

			// Add devices
			#if defined(WIN32) || defined(_WIN32) || defined(__WIN32) && !defined(__CYGWIN__)
				const char alternative_name[] = "Alternative name ";
				found = line.find(alternative_name);
				found += strlen(alternative_name);
				line = line.substr(found);
				devices[i][j] = line;
				if(0 == j)
					num_devices++;
			#else
				line.erase(std::remove(line.begin(),line.end(),'\t'),line.end());
				devices[i][j] = line;
				sprintf(str, "hw:%i", i);
				devices[i][1] = str;
				num_devices++;
			#endif
			i++;
		}
		else
		{
			found = line.find("DirectShow audio devices");
			if (found != std::string::npos)
			{
				i = 0;
				j = 1;
			}
			else
			{
				found = line.find("HD Pro Webcam");
				if (found != std::string::npos)
				{
					guardar = true;
				}
			}
		}
	}


	std::ofstream myfile;
	myfile.open("video_files.txt");

	
	str[0] = '\0';
	sprintf(str, "ffmpeg -f dshow -i audio=\"Microphone(High Definition Audio Device)\" -y -strict experimental -acodec aac -copyts test.m4a");
	std::cout << "\n" << str << "\n";
	std::thread(record_audio, str).detach();


	
	
	i = 0;
	while (i < 5)
	{
		// Intializes random number generator 
		time_t t;
		srand((unsigned)time(&t));

		// Get next scene duration
		timer = rand() % 6000 + 8000; // timer for long time (3-5 seconds)
		timer_seconds = (timer / 1000);
		// Get next scene camera
		current_camera = change_camera(num_devices, current_camera);
		// Start scene recording
		str[0] = '\0';
		const char* video;
		const char* audio;
		video = devices[current_camera][0].c_str();
		audio = devices[current_camera][1].c_str();

		#if defined(WIN32) || defined(_WIN32) || defined(__WIN32) && !defined(__CYGWIN__)	
		sprintf(str, "ffmpeg -s 1920x1080 -f dshow -vcodec h264 -i video=%s -copyinkf -vcodec copy -bsf:v h264_mp4toannexb -copyts -f mpegts -t %i -y video%i.ts 2>&1", video, timer_seconds, i);
		//sprintf(str, "ffmpeg -s 1920x1080 -f %s -vcodec h264 -i video=%s -i audio=%s -copyinkf -codec copy -t %i -y video%i.%s ", API_name, video, audio, timer_seconds, i, recording_file_extension);
		#else
			//sprintf(str, "ffmpeg -s 1920x1080 -f %s -vcodec h264 -r 30 -i video=\"%s\" -f alsa -i %s -copyinkf -vcodec copy -t %i -y video%i.mp4", API_name, video, audio, timer_seconds, i);

		


		#endif

		std::thread (record, str, i).detach();
		//result = exec_command(str);

		

		i++; 

		//usleep(timer_seconds * 1000 * 1000);
		int timer_difference = (timer_seconds * 1000) - 500;
		std::this_thread::sleep_for(std::chrono::microseconds(timer_difference * 1000));
	}

	// For the last video, we sleep the process until the timer is reached
	//usleep(timer_seconds * 1000 * 1000);
	std::this_thread::sleep_for(std::chrono::microseconds(5 * 1000 * 1000));



	video_count = i;

	

	//video_count = 40;



	std::cout << "\n" << "Press q on FFMPEG window to continue" << "\n";
	std::cin.get();





	double start_times[1024];

	for (i = 0; i < video_count; i++)
	{
		str[0] = '\0';
		sprintf(str, "video%i_start.txt", i);

		std::string line;
		std::ifstream start_file(str);
		if (start_file.is_open())
		{
			while (std::getline(start_file, line))
			{
				start_times[i] = std::stod(line, NULL);
				std::cout << start_times[i] << "\n";
			}
			start_file.close();
		}

		std::cout << "\n";

		str[0] = '\0';
		sprintf(str, "file 'video%i_rem.%s'\n", i, recording_file_extension);
		myfile << str;
	}
	
	myfile.close();

	double duracion = 0.0;

	for (i = 0; i < (video_count - 1); i++)
	{
		duracion = start_times[i + 1] - start_times[i];

		int duration = duracion * 1000;

		int msec = duration % 1000;
		duration = duration / 1000;
		int hours = duration / 3600;
		int minutes = (duration - (hours * 3600)) / 60;
		int seconds = (duration - (hours * 3600) - (minutes * 60));

		str[0] = '\0';
		sprintf(str, "ffmpeg -t %02i:%02i:%02i.%03i -i video%i_temp.%s -c copy -y video%i_rem.%s", hours, minutes, seconds, msec, i, recording_file_extension, i, recording_file_extension);
		std::cout << "\n" << str << "\n";
		//std::cin.get();
		result = exec_command(str);

		/*
		str[0] = '\0';
		sprintf(str, "avidemux_cli --force-alt-h264 --load \"video%i_temp.%s\" --save \"video%i_rem.%s\" --output-format MP4 --quit", i, recording_file_extension, i, recording_file_extension);
		std::cout << "\n" << str << "\n";
		result = exec_command(str);
		*/
		
		//std::cin.get();
	}

	double diff = start_times[video_count - 1][1] - start_times[video_count - 1][0] + 0.200;

	str[0] = '\0';
	sprintf(str, "ffmpeg -i video%i.%s -itsoffset %f -i video%i.%s -c copy -map 0:0 -map 1:1 -y video%i_rem.%s", (video_count - 1), recording_file_extension, diff, (video_count - 1), recording_file_extension, (video_count - 1), recording_file_extension);
	std::cout << "\n" << str << "\n";

	result = exec_command(str);

	/*
	str[0] = '\0';
	sprintf(str, "avidemux_cli --force-alt-h264 --load \"video%i_temp.%s\" --save \"video%i_rem.%s\" --audio-delay  --output-format MP4 --quit", i, recording_file_extension, i, recording_file_extension);
	std::cout << "\n" << str << "\n";
	result = exec_command(str);
	*/

	str[0] = '\0';
	sprintf(str, "ffmpeg -f concat -i video_files.txt -c copy -y output.mp4 2>&1");
	result = exec_command(str);

	return 0;
}

