using System;
using System.Drawing;
using Objects.Organization;
using Objects.Other;
using Rhino;
using Rhino.DocObjects;
using Speckle.Core.Kits;
using Speckle.Core.Models;
#if GRASSHOPPER
#endif

namespace Objects.Converter.RhinoGh;

public partial class ConverterRhinoGh
{
  // doc, aka base commit
  public Collection CollectionToSpeckle()
  {
    return new Collection("Rhino Model", "rhino model");
  }

  // layers
  public ApplicationObject CollectionToNative(Collection collection)
  {
    return null;
  }

  public Collection LayerToSpeckle(Layer layer)
  {
    var collection = new Collection(layer.Name, "layer") { applicationId = layer.Id.ToString() };

    // add dynamic rhino props
    collection["visible"] = layer.IsVisible;

    return collection;
  }

  public void SetContextDocument(object doc)
  {
    return;
  }
}
