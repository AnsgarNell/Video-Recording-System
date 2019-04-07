#pragma once

class CamerasManager
{
public:
	CamerasManager(void);
	~CamerasManager(void);
	int ChangeCamera();
private:
	int CountCameras();
};

