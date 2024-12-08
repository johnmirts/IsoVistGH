using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace IsoVistGH {
    public class Painter {
        public static readonly double Tolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
        public static readonly Color Polygon_Hatch_Color = Color.FromArgb(255, 0, 145);
        public static readonly Color Polygon_Border_Color = Polygon_Hatch_Color;

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // THE PAINTER OBJECT
        /////////////////////////////////////////////////////////////////////////////////////////////////

        private Tuple<Curve, Hatch, Color, Color, int> Polygon;
        private BoundingBox BBox;

        public BoundingBox BoundingBox { get { return BBox; } }

        public Painter() {
            Polygon = null;
            BBox = BoundingBox.Empty;
        }
        public void Clear() {
            Polygon = null;
            BBox = BoundingBox.Empty;
        }
        public void Init() {
            Clear();
        }
        public void Draw(IGH_PreviewArgs args) {
            Tuple<Curve, Hatch, Color, Color, int> regionsInTup = Polygon;
            args.Display.DrawHatch(regionsInTup.Item2, regionsInTup.Item3, regionsInTup.Item4);
            args.Display.DrawCurve(regionsInTup.Item1, regionsInTup.Item4, regionsInTup.Item5);
        }
        public void Bake(RhinoDoc doc, List<Guid> obj_ids) {
            //TODO
        }
        public void BuildPolygon(Curve crv, int seg_thickness, double hatch_scale, double hatch_rotation) {
            int index = RhinoDoc.ActiveDoc.HatchPatterns.Find("Hatch1", true);
            Hatch hatch = Hatch.Create(crv, index, hatch_rotation, hatch_scale, Tolerance)[0];
            Polygon = new Tuple<Curve, Hatch, Color, Color, int>(crv, hatch, Polygon_Hatch_Color, Polygon_Border_Color, seg_thickness);
        }
    }
}
