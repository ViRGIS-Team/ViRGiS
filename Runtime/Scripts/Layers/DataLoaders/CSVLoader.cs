using System.Threading.Tasks;
using System.Collections.Generic;
using System.Dynamic;
using System.Data;
using System.IO;
using Project;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration;
using System.Collections;

namespace Virgis {

    public class CsvLoader : DataLoaderPrototype {

        public override async Task _init() {
            IsWriteable = true;
            features = new();

            CsvConfiguration config = new(CultureInfo.InvariantCulture) {
                DetectDelimiter = true,
                DetectDelimiterValues= new[] { ",", ";", "|", "\t", " " }
            };
            using (StreamReader reader = new((Layer as RecordSet).Source))
            using (CsvReader csv = new(reader, config))
            {
                using (CsvDataReader dr = new(csv))
                {
                    features.Load(dr);
                    DataColumn fid = new ("__FID", typeof(long));
                    features.Columns.Add(fid);
                    long i = 0;
                    foreach (DataRow row in features.Rows) {
                        row.SetField(fid, i);
                        i++;
                    }
                    features.PrimaryKey = new[] { fid };
                    foreach(DataColumn col in features.Columns) {
                        col.ReadOnly = false;
                    }
                }
            }
            await base._init();
        }

        public override Task _save() {
            using (StreamWriter writer = new((Layer as RecordSet).Source))
            using (CsvWriter csv = new(writer, CultureInfo.InvariantCulture)) {
                csv.WriteRecords(Records());
            }
            return Task.CompletedTask;
        }

        protected override IEnumerator Hydrate() {
            throw new System.NotImplementedException();
        }

        private IEnumerable<dynamic> Records() {
            var record = new ExpandoObject() as IDictionary<string, object>;
            foreach (DataColumn col in features.Columns) {
                if (col.ColumnName != "__FID")
                    record.Add(col.ColumnName, "");
            }
            foreach (DataRow row in features.Rows) {
                foreach(DataColumn col in features.Columns) {
                    if (col.ColumnName != "__FID") record[col.ColumnName] =  row[col];
                }
                yield return record;
            }
        }
    }
}
