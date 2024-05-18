using System.Threading.Tasks;
using System.IO;
using Project;
using CsvHelper;
using System.Globalization;

namespace Virgis {

    public class CSVLoader : DataLoaderPrototype {

        public override async Task _init() {
            isWriteable = true;
            features = new();
            using (StreamReader reader = new((_layer as RecordSet).Source))
            using (CsvReader csv = new(reader, CultureInfo.InvariantCulture))
            {
                using (CsvDataReader dr = new(csv))
                {
                    features.Load(dr);
                }
            }
            base._init();
        }
    }
}
