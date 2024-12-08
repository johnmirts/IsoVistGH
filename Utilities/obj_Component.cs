using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Rhino.Geometry;
using Rhino;
using System.Collections.Generic;
using System;
using System.Drawing;

namespace IsoVistGH {
    public abstract class BaseComponent : GH_Component {

        private readonly Bitmap icon = null;

        public BaseComponent(string _componentName, string _nickname, string _description, Bitmap _icon)
          : base(_componentName, _nickname, _description, "IsoVistGH", "Standard") {
            icon = _icon;
            IconDisplayMode = GH_IconDisplayMode.icon;
        }

        protected override void BeforeSolveInstance() {
            base.BeforeSolveInstance();
            var plugin = new IsoVistGHInfo();
            Message = plugin.Version;
        }

        public override void CreateAttributes() {
            base.m_attributes = new BaseComponentAttributes(this);
        }

        protected override Bitmap Icon {
            get {
                return icon;
            }
        }
    }
    internal class BaseComponentAttributes : GH_ComponentAttributes {
        internal BaseComponentAttributes(IGH_Component component)
          : base(component) { }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel) {
            if (channel == GH_CanvasChannel.Objects) {
                // Cache the existing style.
                GH_PaletteStyle style = GH_Skin.palette_normal_standard;

                // Swap out palette for normal, unselected components.
                GH_Skin.palette_normal_standard = new GH_PaletteStyle(Color.FromArgb(225, 225, 225), Color.Black, Color.Black);

                base.Render(canvas, graphics, channel);

                // Put the original style back.
                GH_Skin.palette_normal_standard = style;
            }
            else
                base.Render(canvas, graphics, channel);
        }
    }
    public abstract class HighlightCurveComponent : BaseComponent {
        // For previewing the domain
        protected Painter painter;

        // Initializes a new instance of the BaseComponent class
        public HighlightCurveComponent(string _componentName, string _nickname, string _description, Bitmap _icon)
          : base(_componentName, _nickname, _description, _icon) {
            painter = new Painter();
        }

        // Cleans the scene in order to display the geometries/colors of the model
        protected override void BeforeSolveInstance() {
            base.BeforeSolveInstance();
            painter.Clear();
        }

        // For displaying the preview Geometry
        public override BoundingBox ClippingBox { get { return BoundingBox.Union(base.ClippingBox, painter.BoundingBox); } }

        // For displaying the preview Geometry
        public override void DrawViewportWires(IGH_PreviewArgs args) {
            base.DrawViewportWires(args);
            painter.Draw(args);
        }
        public override void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids) {
            painter.Bake(doc, obj_ids);
        }
    }
}
