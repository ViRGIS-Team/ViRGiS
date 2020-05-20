using Project;
using System.Threading.Tasks;

namespace Virgis {

    public class TabLayer : Layer<GeologyCollection, DataPlotter>
    {
 

        protected override Task _init(GeologyCollection layer) {
            
        }

        protected override void _draw() {
            
        }

        protected override void _checkpoint() {
            
        }

        protected override void _save() {
            throw new System.NotImplementedException();
        }

        protected override void _addFeature(MoveArgs args) {
            throw new System.NotImplementedException();
        }

        public override void MoveAxis(MoveArgs args) {
            
        }

        public override void Translate(MoveArgs args) {
            
        }

    }
}
