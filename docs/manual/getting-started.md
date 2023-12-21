# Getting started

This page will cover the basics of using the Midas package.


## Basic Usage

To start, create a new 'Midas' instance:

```CSharp
var midas = new Midas();
```

Then you can pass in a Texture2D to @Midas.Midas.EstimateDepth(UnityEngine.Texture,System.Boolean)

```CSharp
// Estimate depth from an input texture
var predictedDepth = midas.EstimateDepth(inputImage);

// ... use predictedDepth as needed
```

Finally, when you're done you should call 'Dispose()' to properly free up native memory resources.

```CSharp
midas.Dispose();
```

You can reuse the same 'Midas' isntance for multiple inferences. But if you only need it once, you might want to use the 'using' statement, which automatically Dispose() for you:

```CSharp
using (Midas midas = new Midas()) {
    // your code
}
```

A simple example on how to use the library can also be found in the 'Basic Sample' that can be imported via the Package Manager.


## Choosing a Model Type

Use the ModelType property to set the desired Midas model. Default is @Midas.ModelType.midas_v21_small_256.

```CSharp
var midas = new Midas(ModelType.dpt_beit_large_384);
```

Changing the model type on an existing 'Midas' instance automatically disposes of the existing model and initializes the new one.

```CSharp
midas.ModelType = ModelType.midas_v21_small_256;
```


## Choosing a Backend

Use the Backend property to set the desired backend for model execution (GPUCompute by default).
Changing the backend automatically disposes of the existing model and initializes the new one.

```CSharp
midas.Backend = BackendType.CPU;
```


## Normalizing Depth

Set the NormalizeDepth property to true if you want to normalize the estimated depth values between 0 and 1. This is mainly useful for visualization.