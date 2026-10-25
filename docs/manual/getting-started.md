# Getting started

This page will cover the basics of using the Midas package.

## Get The Models

The package does not contain model files. It loads them from the Hugging Face repository
[julienkay/sentis-MiDaS](https://huggingface.co/julienkay/sentis-MiDaS) through AI Model Hub
(`tools.doji.hf-hub`), which Package Manager installs with the package.

1. Open `Window > AI Model Hub`.
2. Select `julienkay/sentis-MiDaS`. `midas_v21_small_256` is always included. Turn on the other models you
   want to use under **Optional files**.
3. Click **Download & Import**.

Builds bundle the imported models by default. To download them in the player instead, see the
[AI Model Hub manual][hf_hub_docs]. A player that downloads ONNX models also needs the ONNX Model Loader
package (`tools.doji.onnx-model-loader`) to convert them.


## Basic Usage

To start, create a 'Midas' instance. `CreateAsync` works on every platform, also when the model is
downloaded at runtime:

```CSharp
Midas midas = await Midas.CreateAsync();
```

The constructor loads the model without waiting. It works in the Editor and for bundled models on Windows,
macOS, Linux and iOS:

```CSharp
var midas = new Midas();
```

Then you can pass in a Texture2D to @Doji.AI.Depth.Midas.EstimateDepth(Texture,System.Boolean)

```CSharp
// Estimate depth from an input texture
midas.EstimateDepth(inputImage);
RenderTexture predictedDepth = midas.Result;

// ... use predictedDepth as needed
```

Finally, when you're done you should call 'Dispose()' to properly free up native memory resources.

```CSharp
midas.Dispose();
```

You can reuse the same 'Midas' instance for multiple inferences. But if you only need it once, you might want to use the 'using' statement, so you don't need to worry about disposing it:

```CSharp
using (Midas midas = new Midas()) {
    // your code
}
```

A simple example on how to use the library can also be found in the 'Basic Sample' that can be imported via the Package Manager.


## Choosing a Model Type

The default model is @Doji.AI.Depth.ModelType.midas_v21_small_256. You can specify the model to be used in the Midas constructor.

```CSharp
var midas = new Midas(ModelType.dpt_beit_large_384);
```

You can also change the model on an existing 'Midas' instance through the @Doji.AI.Depth.ModelType property. Changing the model automatically disposes of the existing model and loads the new one without waiting, like the constructor.

```CSharp
midas.ModelType = ModelType.dpt_beit_large_384;
```

Note, that different models can have different output sizes, so changing the model might change the dimensions of the predicted depth map.

For more information on available models and when to use which, see the [Models](~/manual/models.md) page.


## Choosing a Backend

Use the Backend property to set the desired backend for model execution (GPUCompute by default).
Changing the backend creates a new worker for the loaded model.

```CSharp
midas.Backend = BackendType.CPU;
```


## Normalizing Depth

Set the NormalizeDepth property to true if you want to normalize the estimated depth values between 0 and 1. This is mainly useful for visualization.

[hf_hub_docs]: https://docs.doji-tech.com/tools.doji.hf-hub/manual/getting-started-project-users.html
