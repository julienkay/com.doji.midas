# Getting started

This page will cover the basics of using the Midas package.

## Basic Usage

```CSharp
using UnityEngine;

public class DepthEstimationExample : MonoBehaviour {
    public Texture inputImage;
    public Midas midasInstance;

    void Start() {
        // Initialize Midas with default parameters
        midasInstance = new Midas();

    }

    void EstimateDepth() {
        // Estimate depth from input texture
        midasInstance.EstimateDepth(inputImage);

        // Access depth result for further processing or visualization
        RenderTexture depthResult = midasInstance.Result;

        // Use depthResult as needed

    }
}
```

## Accessing Depth Results

The depth estimation results are stored in the Result property of the Midas class. This property is a RenderTexture containing the estimated depth.

## Choosing a Model Type

Use the ModelType property to set the desired Midas model. Default is midas_v21_small_256.
Changing the model type automatically disposes of the existing model and initializes the new one.

## Choosing a Backend

Use the Backend property to set the desired backend for model execution (GPUCompute by default).
Changing the backend automatically disposes of the existing model and initializes the new one.

## Normalizing Depth (Optional)

Set the NormalizeDepth property to true if you want to normalize the estimated depth values between 0 and 1. This is mainly useful for visualization.

A simple example on how to use the library can be found in the 'Basic Sample' that can be imported via the Package Manager.