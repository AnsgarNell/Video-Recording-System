#!/bin/bash
cd /home/pi/FFMPEG/
espeak "System started"
read -n1 -t 10 -p "Abort cameras script? [y,n]" input
case $input in
y|Y)
echo
cd ..
cd VLC
;;
*)
for i in {1..30}
do
	./a.out
	file_date=$(date +%d-%m-%Y_%H-%M);
	#ffmpeg -i concat.h264 -i concat.mp3 -vcodec copy -acodec copy -y output_"$file_date".mp4
	#ffmpeg -i concat.h264 -vcodec copy -y -r 30 output_"$file_date".mp4
	ffmpeg -f concat -i video_files.txt -c copy -y output_"$file_date".mp4
	espeak "Recording $i finished"
done
espeak "Shutting down"
#sudo shutdown -h now;;
esac
