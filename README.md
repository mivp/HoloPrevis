# Hololen app to view previs data

## Current functions (master branch)

- Load a previs mesh model by scanning a QR code. The model data is stored locally (StreamingAssets)
- Provide a GUI menu to load/unload, scan QR code and modify the loaded object (move, rotate, scale)
- Support multi-hololens using UNET (only user who loads object can unload)
- Highligh object part when a user focus on it


## Bugs or improvement features

- Add network download
- Add sounds (environment + interaction)

## Dev enviroment

- Windows 10
- Unity 2018.3
- HoloToolkit-Unity-2017.4.3.0
- Visual studio 2017 (community version)

## How to build and run

- Load project folder in Unity
- Open HoloPrevis scene
- Build UWP project
- Build and deploy to hololens
