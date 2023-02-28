/* MIT License

Copyright (c) 2020 - 21 Runette Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice (and subsidiary notices) shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */

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
            if (project.Guid == Guid.Empty) {
                project.Guid = Guid.NewGuid();
                SetProject(project);
                Save();
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
