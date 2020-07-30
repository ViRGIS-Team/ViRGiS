using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using g3;
using Microsoft.Win32.SafeHandles;
using OSGeo.OSR;



namespace Mdal {


    public class Mdal {

        private const string MDAL_LIBRARY = "mdal.dll";

        [DllImport(MDAL_LIBRARY, EntryPoint = "MDAL_Version")]
        private static extern IntPtr MDAL_Version();

        public static string GetVersion() {
            IntPtr ret = MDAL_Version();
            string version = Marshal.PtrToStringAnsi(ret);
            return version;
        }

        [DllImport(MDAL_LIBRARY, EntryPoint = "MDAL_LastStatus")]
        private static extern int MDAL_lastStatus();

        public static MDAL_Status LastStatus() {
            int ret = MDAL_lastStatus();
            return (MDAL_Status) ret;
        }

        [DllImport(MDAL_LIBRARY, EntryPoint = "MDAL_LoadMesh")]
        public static extern MdalMesh MDAL_LoadMesh([MarshalAs(UnmanagedType.LPStr)] StringBuilder uri);

        [DllImport(MDAL_LIBRARY, EntryPoint = "MDAL_MeshNames")]
        private static extern IntPtr MDAL_MeshNames([MarshalAs(UnmanagedType.LPStr)] StringBuilder uri);

        public static string GetNames(string uri) {
            StringBuilder stb = new StringBuilder(uri);
            IntPtr ret = MDAL_MeshNames(stb);
            return Marshal.PtrToStringAnsi(ret); 
        }

        [DllImport(MDAL_LIBRARY, EntryPoint = "MDAL_M_vertexCount")]
        public static extern int MDAL_M_vertexCount(MdalMesh pointer);

        [DllImport(MDAL_LIBRARY, EntryPoint = "MDAL_M_edgeCount")]
        public static extern int MDAL_M_edgeCount(MdalMesh pointer);

        [DllImport(MDAL_LIBRARY, EntryPoint = "MDAL_M_faceCount")]
        public static extern int MDAL_M_faceCount(MdalMesh pointer);

        [DllImport(MDAL_LIBRARY, EntryPoint = "MDAL_M_projection")]
        private static extern IntPtr MDAL_M_projection(MdalMesh pointer);

        public static string GetCRS(MdalMesh pointer) {
            IntPtr ret = MDAL_M_projection(pointer);
            string proj = Marshal.PtrToStringAnsi(ret);
            return proj;
        }

        [DllImport(MDAL_LIBRARY, EntryPoint = "MDAL_M_vertexIterator")]
        public static extern MdalVertexIterator MDAL_M_vertexIterator(MdalMesh pointer);

        [DllImport(MDAL_LIBRARY, EntryPoint = "MDAL_VI_next")]
        public static extern int MDAL_VI_next(MdalVertexIterator pointer, int verticesCount,  double[] coordinates);

        [DllImport(MDAL_LIBRARY, EntryPoint = "MDAL_VI_close")]
        public static extern void MDAL_VI_close(MdalVertexIterator pointer);

        [DllImport(MDAL_LIBRARY, EntryPoint = "MDAL_M_faceIterator")]
        public static extern MdalFaceIterator MDAL_M_faceIterator(MdalMesh pointer);

        [DllImport(MDAL_LIBRARY, EntryPoint = "MDAL_FI_next")]
        public static extern int MDAL_FI_next(MdalFaceIterator iterator,
                              int faceOffsetsBufferLen,[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]  int[] faceOffsetsBuffer,
                              int vertexIndicesBufferLen,
                              [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] int[] vertexIndicesBuffer);

        [DllImport(MDAL_LIBRARY, EntryPoint = "MDAL_FI_close")]
        public static extern void MDAL_FI_close(MdalFaceIterator pointer);

        [DllImport(MDAL_LIBRARY, EntryPoint = "MDAL_M_faceVerticesMaximumCount")]
        public static extern int MDAL_M_faceVerticesMaximumCount(MdalMesh pointer);


        

    }

    public class Datasource {
        string uri;
        public string[] meshes;

        public Datasource(string uri) {
            this.uri = uri;
            string ret = Mdal.GetNames(uri);
            MDAL_Status status = Mdal.LastStatus();
            if (ret == null &&  status != MDAL_Status.None)
                throw new Exception(status.ToString() + uri);
            meshes = ret.Split( new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public MdalMesh GetMesh(int index) {
            StringBuilder stb = new StringBuilder(meshes[index]);
            return Mdal.MDAL_LoadMesh(stb);
        }

    }

    public sealed class MdalMesh : SafeHandleZeroOrMinusOneIsInvalid {

        public MdalMesh() : base(ownsHandle: true) {

        }

        protected override bool ReleaseHandle() {
            return true;
        }

        private SimpleMesh ToMesh() {
            int vcount = Mdal.MDAL_M_vertexCount(this);
            MdalVertexIterator vi = Mdal.MDAL_M_vertexIterator(this);
            VectorArray3d v = vi.GetVertexes(vcount);
            SimpleMesh mesh = new SimpleMesh();
            mesh.AppendVertices(v);
            int fcount = Mdal.MDAL_M_faceCount(this);
            MdalFaceIterator fi = Mdal.MDAL_M_faceIterator(this);
            IndexArray3i tri = fi.GetFaces(fcount, Mdal.MDAL_M_faceVerticesMaximumCount(this));
            mesh.AppendTriangles(tri);
            return mesh;
        }

        private DMesh3 ToDMesh(SpatialReference fixCRS = null) {
            DMesh3 mesh = new DMesh3(this.ToMesh(), MeshHints.None, MeshComponents.None);
            string CRS = Mdal.GetCRS(this);
            if (CRS != null)
                mesh.AttachMetadata("CRS", CRS);
            return mesh;
        }

        public static implicit operator SimpleMesh(MdalMesh thisMesh) => thisMesh.ToMesh();
        public static implicit operator DMesh3(MdalMesh thisMesh) => thisMesh.ToDMesh();
    }

    public sealed class MdalVertexIterator : SafeHandleZeroOrMinusOneIsInvalid {
        
        public MdalVertexIterator() : base (ownsHandle: true) {

        }

        protected override bool ReleaseHandle() {
            Mdal.MDAL_VI_close(this);
            return Mdal.LastStatus() == 0;
        }

        public VectorArray3d GetVertexes(int count) {
            double[] vertexes = new double[count * 3];
            Mdal.MDAL_VI_next(this, count, vertexes);
            return new VectorArray3d(vertexes);
        }
    }

    public sealed class MdalFaceIterator : SafeHandleZeroOrMinusOneIsInvalid {

        public MdalFaceIterator() : base(ownsHandle: true) {

        }

        protected override bool ReleaseHandle() {
            Mdal.MDAL_FI_close(this);
            return Mdal.LastStatus() == 0;
        }

        public IndexArray3i GetFaces(int count, int faceSize) {
            if (faceSize > 4)
                throw new NotSupportedException("Only Tri and Quad Meshes supported");
            int[] faces = new int[count * faceSize];
            int[] faceOff = new int[count];
            int rcount = Mdal.MDAL_FI_next(this, count, faceOff, count * faceSize, faces);
            if (faceSize == 4) {
                List<int> ret = new List<int>();
                int previous = 0;
                for (int i = 0; i < count; i++) {
                    int current = faceOff[i];
                    int size = current - previous;
                    ret.Add(faces[previous]);
                    ret.Add(faces[previous + 1]);
                    ret.Add(faces[previous + 2]);
                    if (size == 4) {
                        ret.Add(faces[previous + 2]);
                        ret.Add(faces[previous + 3]);
                        ret.Add(faces[previous]);
                    }
                    previous = current;
                }
                return new IndexArray3i(ret.ToArray());
            }
            return new IndexArray3i(faces);
        }
    }


    public enum MDAL_Status {
        None,
        Err_NotEnoughMemory,
        Err_FileNotFound,
        Err_UnknownFormat,
        Err_IncompatibleMesh,
        Err_InvalidData,
        Err_IncompatibleDataset,
        Err_IncompatibleDatasetGroup,
        Err_MissingDriver,
        Err_MissingDriverCapability,
        Err_FailToWriteToDisk,
        Err_UnsupportedElement,
        Warn_InvalidElements,
        Warn_ElementWithInvalidNode,
        Warn_ElementNotUnique,
        Warn_NodeNotUnique,
        Warn_MultipleMeshesInFile,
    }

    public enum MDAL_LogLevel {
        Error,
        Warn,
        Info,
        Debug
    }

    /// <summary>
    /// Specifies where the data is defined.
    /// </summary>
    public enum MDAL_DataLocation {
        DataInvalidLocation, //Unknown/Invalid location.
        DataOnVertices, //Data is defined on vertices of 1D or 2D mesh.
        DataOnFaces,  //Data is defined on face centres of 2D mesh.
        DataOnVolumes, //Data is defined on volume centres of 3D mesh.
        DataOnEdges,  //Data is defined on edges of 1D mesh.
    }

    /// <summary>
    /// Data type to be returned by MDAL_D_data.
    /// </summary>
    public enum MDAL_DataType {
        SCALAR_DOUBLE,  //Double value for scalar datasets (DataOnVertices or DataOnFaces or DataOnEdges)
        VECTOR_2D_DOUBLE, //Double, double value for vector datasets(DataOnVertices or DataOnFaces or DataOnEdges)
        ACTIVE_INTEGER,  //Integer, active flag for dataset faces.Some formats support switching off the element for particular timestep (see MDAL_D_hasActiveFlagCapability)
        VERTICAL_LEVEL_COUNT_INTEGER, //Number of vertical level for particular mesh’s face in 3D Stacked Meshes (DataOnVolumes)
        VERTICAL_LEVEL_DOUBLE,  //Vertical level extrusion for particular mesh’s face in 3D Stacked Meshes (DataOnVolumes)
        FACE_INDEX_TO_VOLUME_INDEX_INTEGER, //The first index of 3D volume for particular mesh’s face in 3D Stacked Meshes (DataOnVolumes)
        SCALAR_VOLUMES_DOUBLE, //Double scalar values for volumes in 3D Stacked Meshes (DataOnVolumes)
        VECTOR_2D_VOLUMES_DOUBLE, //Double, double value for volumes in 3D Stacked Meshes (DataOnVolumes)
    }
}
