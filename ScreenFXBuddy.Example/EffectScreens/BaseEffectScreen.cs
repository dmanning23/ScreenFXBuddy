using System.Net.Http.Headers;
using System.Threading.Tasks;
using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using ResolutionBuddy;

namespace ScreenFXBuddy.Example
{
    abstract class BaseEffectScreen : WidgetScreen
    {
        public BaseEffectScreen(string name) : base(name)
        {
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();

            //Add a label in the upper center
            var screenNameLabel = new Label(ScreenName, this.Content)
            {
                Clickable = false,
                Highlightable = false,
                Horizontal = HorizontalAlignment.Center,
                Vertical = VerticalAlignment.Top,
                Position = new Point(Resolution.TitleSafeArea.Center.X, 0),
            };
            AddItem(screenNameLabel);
        }

    }
}