# SamNet

WPF desktop application for **Segment Anything Model 2** and **Segment Anything Model 3** using the high-performance [sam3.cpp](https://github.com/PABannier/sam3.cpp) C++ library (ggml-based, no Python / PyTorch required).

![SamNet Image Segmentation Demo](SamNetPrivate/Media/TomatoSegment.gif)

<a href="https://www.vecteezy.com/free-photos/gardening">Gardening Stock photos by Vecteezy</a>

![SamNet Image Segmentation Demo](SamNetPrivate/Media/TomatoDataSet.png)

## Features

- **SAM 2 / SAM 2.1 style interactive segmentation**
  - Positive / negative point prompts
  - Bounding box prompts
  - Multi-mask output
- **SAM 3 Promptable Concept Segmentation (PCS)**
  - Text prompts (e.g. “cat”, “yellow school bus”)
  - Optional positive / negative boxes with text
- **Manual Editing capabilities to fine tune segmentations**
  - Add or subtract regions from semantic mask
  - Split regions into 2 labels where they overlap
  - Eliminate small background detections
- Native C++ inference via `sam3.dll` + ggml backends
- Emgu.CV for image loading, mask visualization, and post-processing
- Multi-page WPF UI for experimentation and annotation-style workflows

## Requirements

- Windows (x64)
- .NET (version 10.0)
- Visual Studio 2026 (or compatible) recommended for building
- One or more SAM models in the format expected by sam3.cpp (GGUF / converted weights)
- Five DLLs described below under "Quick Start" and found in repositories with links in "Dependencies"

## Quick Start

1. Clone the repository
2. Place the required native DLLs in `SamNet/Lib/` (already included for convenience):
   - `sam3.dll`
   - `ggml.dll`, `ggml-base.dll`, `ggml-cpu.dll`
   - `SamNet.Native.dll`
3. Download a compatible model from the [sam3.cpp model zoo](https://huggingface.co/PABannier/sam3.cpp) and place it somewhere accessible
4. Open `SamImageSharp.slnx` in Visual Studio and build/run
5. In the app:
   - Load an image
   - Load a model
   - Use points / boxes for classic SAM-style segmentation  
     **or** enter a text prompt for SAM 3 concept segmentation

     Alternate workflows:
   - Start a manual semantic segmentation
   - Import saved Coco Rle style saved segmentations

## Project Structure

SamNet/  
├── SamNet/          # Main WPF application   
│   ├── Lib/                # Native DLLs (sam3 + ggml)  
│   ├── SamModel.cs         # Shared state & mask helpers   
│   └── ...  
├── OpenIP/                 # Supporting image-processing helpers  
└── README.md
└── LICENSE

## Supported Prompt Modes

| Mode | Description |
|------|-------------|
| Points | Left-click = positive, right-click = negative |
| Boxes  | Draw bounding boxes |
| Multi-mask | Request multiple mask candidates |
| Text (PCS) | Open-vocabulary concept segmentation (“find every …”) |
| Text + Boxes | Combine text prompt with geometric constraints |

## Fine-tune Semantic Segmentation

- Segmentation is a critical step in feeding images into a model for processing.  Defects can cause erroneous results
- Simple example of manual fine tuning below:
  
![SamNet Image Segmentation Demo](SamNetPrivate/Media/TomatoEdit.gif)

<a href="https://www.vecteezy.com/free-photos/gardening">Gardening Stock photos by Vecteezy</a>

- Frame 1 - the original image of a segmentation within its bounding box
- Frame 2 - segmentation completed by Sam3 based on the prompt "Green Plants"
- Frame 3 - subtraction of selected areas that need fine tuning (not precise)
- Frame 4 - addition of precise edits to segmentation
- Frame 5 - improved segmentation

**Editing Capabilities provided**

- adding segmented area (by drawing area with mouse)
- subtracting segmented area
- splitting a segmented area into 2 parts
- auto removal of small segmentations  
- automated fine tuning is not provided in this release
- context based automated fine tuning can be created commercially
- context based automated re-labelling can be created commercially

## Export Capabilities

- Coco Rle format to preserve segmentations for editing/feed to models
 
├── ExportFolder /          # Folder chosen to hold the export  
│   ├── Images/             # The original image used  
│   ├── Annotations/        # Annotation information for the image    
│   └── PNG_Masks/          # Png image of complete mask for the original image

- Format above is limited to 1 image per Coco format with local naming
- Customized formats with multiple image workflows can be created commercially

## Notes

- Image encoding is performed from tightly-packed RGB/RGBA buffers.
- Masks are returned as single-channel binary images and can be combined / outlined for visualization.
- The current focus is **image** segmentation. Video tracking (memory bank) support from sam3.cpp is not yet exposed in the UI.

## Dependencies

- [sam3.cpp](https://github.com/rwmcclellan/sam3.cpp) – core inference engine - MIT license
- Emgu.CV – OpenCV wrapper for .NET
- ApexIP – internal image-processing helpers
- [SamNet.Native](https://github.com/rwmcclellan/SamNet.Native) - C# wrapper for sam3.cpp - MIT license

## License

GNU General Public License v3.0 License – see [LICENSE](LICENSE)

## Acknowledgements

[sam3.cpp](https://github.com/pabannier/sam3.cpp)  by PABannier  
Meta AI for the original Segment Anything models  
AI support from Grok, Pe
