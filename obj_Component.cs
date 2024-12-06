using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using System.Drawing;

namespace IsoVistGH {
    public abstract class BaseComponent : GH_Component {

        private readonly GH_Exposure exposure = GH_Exposure.hidden;
        private readonly Bitmap icon = null;

        public BaseComponent(string _componentName, string _nickname, string _description, string _subcategory, GH_Exposure _exposure, Bitmap _icon)
          : base(_componentName, _nickname, _description, "IsoVistGH", "Standard") {
            exposure = _exposure;
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

        public override GH_Exposure Exposure {
            get { return exposure; }
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
}
