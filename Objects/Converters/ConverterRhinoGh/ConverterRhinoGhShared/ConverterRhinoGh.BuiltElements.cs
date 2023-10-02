#if GRASSHOPPER
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using Objects.BuiltElements;
using Objects.Geometry;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using RH = Rhino.Geometry;
using RV = Objects.BuiltElements.Revit;

namespace Objects.Converter.RhinoGh;

public partial class ConverterRhinoGh
{
  // parameters
  public Tuple<string, string> ParameterToNative(RV.Parameter parameter)
  {
    var name = parameter.name;
    var val = parameter.value?.ToString() ?? string.Empty;
    return new Tuple<string, string>(name, val);
  }

  // views
  public View3D ViewToSpeckle(ViewInfo view)
  {
    // get orientation vectors
    var up = view.Viewport.CameraUp;
    var forward = view.Viewport.CameraDirection;
    up.Unitize();
    forward.Unitize();

    var _view = new View3D();
    _view.name = view.Name;
    _view.upDirection = new Vector(up.X, up.Y, up.Z, "none");
    _view.forwardDirection = new Vector(forward.X, forward.Y, forward.Z, "none");
    _view.origin = PointToSpeckle(view.Viewport.CameraLocation);
    _view.target = PointToSpeckle(view.Viewport.TargetPoint);
    _view.isOrthogonal = view.Viewport.IsParallelProjection ? true : false;
    _view.units = ModelUnits;

    // get view bounding box
    var near = view.Viewport.GetNearPlaneCorners();
    var far = view.Viewport.GetFarPlaneCorners();
    if (near.Length > 0 && far.Length > 0)
    {
      var box = new RH.Box(new RH.BoundingBox(near[0], far[3]));
      _view.boundingBox = BoxToSpeckle(box);
    }

    // attach props
    AttachViewParams(_view, view);

    return _view;
  }

  public ApplicationObject ViewToNative(View3D view)
  {
    return null;
  }

  private void AttachViewParams(Base speckleView, ViewInfo view)
  {
    // lens
    speckleView["lens"] = view.Viewport.Camera35mmLensLength;

    // frustrum
    if (
      view.Viewport.GetFrustum(
        out double left,
        out double right,
        out double bottom,
        out double top,
        out double near,
        out double far
      )
    )
      speckleView["frustrum"] = new List<double> { left, right, bottom, top, near, far };

    // crop
    speckleView["cropped"] = bool.FalseString;
  }

  private RhinoViewport SetViewParams(RhinoViewport viewport, Base speckleView)
  {
    // lens
    var lens = speckleView["lens"] as double?;
    if (lens != null)
      viewport.Camera35mmLensLength = (double)lens;

    return viewport;
  }

  // direct shape
  public List<object> DirectShapeToNative(RV.DirectShape directShape, out List<string> log)
  {
    log = new List<string>();
    if (directShape.displayValue == null)
    {
      log.Add($"Skipping DirectShape {directShape.id} because it has no {nameof(directShape.displayValue)}");
      return null;
    }

    if (directShape.displayValue.Count == 0)
    {
      log.Add($"Skipping DirectShape {directShape.id} because {nameof(directShape.displayValue)} was empty");
      return null;
    }

    IEnumerable<object> subObjects = directShape.displayValue.Select(ConvertToNative).Where(e => e != null);

    var nativeObjects = subObjects.ToList();

    if (nativeObjects.Count == 0)
    {
      log.Add(
        $"Skipping DirectShape {directShape.id} because {nameof(directShape.displayValue)} contained no convertable elements"
      );
      return null;
    }

    return nativeObjects;
  }

  // level
  public ApplicationObject LevelToNative(Level level)
  {
    var appObj = new ApplicationObject(level.id, level.speckle_type) { applicationId = level.applicationId };

    var commitInfo = GetCommitInfo();
    var bakedLevelName = ReceiveMode == ReceiveMode.Create ? $"{commitInfo} - {level.name}" : $"{level.name}";

    var elevation = ScaleToNative(level.elevation, level.units);
    var plane = new RH.Plane(new RH.Point3d(0, 0, elevation), RH.Vector3d.ZAxis);

    return appObj;
  }

  #region CIVIL

  // alignment
  public RH.Curve AlignmentToNative(Alignment alignment)
  {
    var curves = new List<RH.Curve>();
    foreach (var entity in alignment.curves)
    {
      var converted = CurveToNative(entity);
      if (converted != null)
        curves.Add(converted);
    }
    if (curves.Count == 0)
      return null;

    // try to join entity curves
    var joined = RH.Curve.JoinCurves(curves);
    return joined.First();
  }

  #endregion
}
