# RDPmonitorselector
A simple utility to select the desired monitors to be part of the rdp experience.
![image](https://github.com/user-attachments/assets/9b24e512-9fff-4aab-9066-3435bdf52256)

1. Can edit an existing configuration file to set which monitors are part of the remote experience. Only contiguous monitors are supported, this is a MS Windows limitation.
2. Can create a new profile saved next to where the app is running, with default parameters.
3. Requires dotnet V8.0.

#### Saves you from the hassle of
1. Checking the coordinates of you monitors using MSTSC /l whenever windows feels like re assigning 'Mstsc IDs' to your monitors.
2. Having to manually edit RDP files.
