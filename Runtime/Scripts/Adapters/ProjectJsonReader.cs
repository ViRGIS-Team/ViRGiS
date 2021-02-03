// copyright Runette Software Ltd, 2020. All rights reserved
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Project;
using System;

namespace Virgis
{


    public class ProjectJsonReader
    {
        public string payload;
        public string fileName;


        public void Load(string file)
        {
            fileName = file;
            char[] result;
            StringBuilder builder = new StringBuilder();
            using (StreamReader reader = File.OpenText(file))
            {
                result = new char[reader.BaseStream.Length];
                reader.Read(result, 0, (int)reader.BaseStream.Length);
                reader.Close();
            }

            foreach (char c in result)
            {
                builder.Append(c);
            }
            payload = builder.ToString();
        }

        public GisProject GetProject()
        {
            GisProject project = JsonConvert.DeserializeObject<GisProject>(payload);
            string test1 = project.ProjectVersion;
            string test2 = GisProject.GetVersion();
            if (project.ProjectVersion != GisProject.GetVersion()) {
                Debug.LogError("This project was created in a different version of ViRGIS and may not be loaded correctly");
            }
            return project;
        }

        public async Task Save()
        {
            using (StreamWriter writer = new StreamWriter(fileName, false))
            {
                await writer.WriteAsync(payload);
            }
        }

        public void SetProject(GisProject project)
        {
            payload = JsonConvert.SerializeObject(project, Formatting.Indented);
        }
    }
}
