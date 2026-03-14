////==================={By Qcbf|qcbf@qq.com|3/27/2022 6:56:48 PM}===================

//using FLib;
//using System;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Rendering;
//using Object = UnityEngine.Object;

//namespace FLib.Unity
//{
//    public class HexMapRenderer : IDisposable
//    {
//        private static readonly bool mIsInstancing = SystemInfo.supportsInstancing;

//        public Material TileMaterial;
//        public Matrix4x4[] Matrices;
//        public MaterialPropertyBlock MatProp;
//        public Vector4[] Colors;
//        private Mesh mTileMesh;
//        public HexMap Map;


//        public HexMapRenderer(Material tileMaterial)
//        {
//            TileMaterial = tileMaterial;
//            MatProp = new MaterialPropertyBlock();
//        }

//        public HexMapRenderer SetMap(HexMap map)
//        {
//            Map = map;
//            if (mTileMesh != null)
//            {
//                Object.DestroyImmediate(mTileMesh);
//            }
//            if (map != null)
//            {
//                mTileMesh = GenerateMesh(map.TileSize);
//            }
//            return this;
//        }

//        public HexMapRenderer Refresh(in Transform parent = null)
//        {
//            var parentMatrix = parent != null ? parent.localToWorldMatrix : Matrix4x4.identity;
//            var size = Map.Tiles.Length;
//            Matrices = new Matrix4x4[size];
//            Colors = new Vector4[size];
//            Array.Fill(Colors, Vector4.one);
//            for (int i = 0; i < size; i++)
//            {
//                Matrices[i] = parentMatrix * Matrix4x4.Translate(Map.MapToWorldPos(Map.IndexToPos(i)).ToUniVec3XZ());
//            }
//            return this;
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        public void DirtyColors()
//        {
//            MatProp.SetVectorArray("_Color", Colors);
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        public void SetColor(in FVector2Int pos, Color col)
//        {
//            Colors[Map.PosToIndex(pos)] = col;
//            DirtyColors();
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        public void ResetColor()
//        {
//            Array.Fill(Colors, Vector4.one);
//            DirtyColors();
//        }

//        public void Draw(Camera cam = null)
//        {
//            if (mIsInstancing)
//            {
//                Graphics.DrawMeshInstanced(mTileMesh, 0, TileMaterial, Matrices, Matrices.Length, MatProp, ShadowCastingMode.Off, false, 0, cam, LightProbeUsage.Off);
//            }
//            else
//            {
//                for (int i = 0; i < Matrices.Length; i++)
//                {
//                    Graphics.DrawMesh(mTileMesh, Matrices[i], TileMaterial, 0, cam, 0, MatProp, false, false, false);
//                }
//            }
//        }


//        public void Dispose()
//        {
//            if (mTileMesh != null)
//            {
//                Object.DestroyImmediate(mTileMesh);
//            }
//        }


//        public static Mesh GenerateMesh(in HexMapTileSize size)
//        {
//            var mesh = new Mesh
//            {
//                vertices = new Vector3[] {
//                    new Vector3(0, 0, 0),
//                    new Vector3(0, 0, -size.Radius),
//                    new Vector3(-size.InnerRadius, 0, -size.Radius * 0.5f),
//                    new Vector3(-size.InnerRadius, 0, size.Radius * 0.5f),
//                    new Vector3(0, 0, size.Radius),
//                    new Vector3(size.InnerRadius, 0, size.Radius * 0.5f),
//                    new Vector3(size.InnerRadius, 0, -size.Radius * 0.5f),
//                },
//                triangles = new int[]
//                {
//                    1, 2, 0,
//                    2, 3, 0,
//                    3, 4, 0,
//                    4, 5, 0,
//                    5, 6, 0,
//                    6, 1, 0,
//                },
//                uv = new Vector2[] {
//                    new Vector2(0.5f, 0.5f),
//                    new Vector2(0.5f, 0f),
//                    new Vector2(0f, 0f),
//                    new Vector2(0f, 1f),
//                    new Vector2(0.5f, 1f),
//                    new Vector2(1f, 1f),
//                    new Vector2(1f, 0),
//                },
//                name = nameof(HexMapRenderer)
//            };
//            return mesh;
//        }


//    }
//}
