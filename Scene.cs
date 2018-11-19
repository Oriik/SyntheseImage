﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SyntheseImage
{
    public class Scene
    {
        public List<Shape> shapes;

        public List<Shape> walls;
        private Tree tree;
        public Camera cam;
        public Light light;
        private Random random;


        public Scene(Camera _cam, Light _light)
        {
            cam = _cam;
            light = _light;
            shapes = new List<Shape>();
            walls = new List<Shape>();
            random = new Random();

        }

        public Image DrawImg(int nbRayonPerPixels)
        {
            Image img = new Image(cam.width, cam.height, "toto3D");

            tree = CreateTree(shapes);
            Console.WriteLine("NB DE SHAPES " + shapes.Count);
            Console.WriteLine("TREE FINISHED");

            for (int y = 0; y < cam.height; y++)
            {
                if (y == 50) break;
            
                for (int x = 0; x < cam.width; x++)
                {
                    Vector3 pointOnCam = new Vector3(cam.origine.X + x, cam.origine.Y + y, cam.origine.Z);
                    Vector3 pixelColor = new Vector3(0, 0, 0);
                    for (int i = 0; i < nbRayonPerPixels; i++)
                    {
                        Rayon rFromCam = new Rayon(pointOnCam, cam.GetFocusAngle(x, y));
                        pixelColor = Vector3.Add(pixelColor, SendRayon(rFromCam));
                    }
                    pixelColor = Vector3.Divide(pixelColor, nbRayonPerPixels);

                    img.SetPixel(x, y, pixelColor.X, pixelColor.Y, pixelColor.Z);
                   
                }
            }


            return img;
        }

        private Tree CreateTree(List<Shape> elements)
        {
            if (elements.Count == 1) return new Tree(elements[0]);
            else
            {
                Box b = elements[0].GetBoundingBox();
                for (int i = 1; i < elements.Count; i++)
                {
                    b = b.Fusion(elements[i].GetBoundingBox());
                }
                elements = elements.OrderBy(s => s.GetBoundingBox().pMin.X).ToList();

                List<Shape> leftElements = elements.GetRange(0, elements.Count / 2);
                List<Shape> rightElements = elements.GetRange(elements.Count/2, elements.Count / 2);

                return new Tree(b, CreateTree(leftElements), CreateTree(rightElements));                
            }
        }

        private Vector3 SendRayon(Rayon rFromCam, int cpt = 0)
        {            
            ResFindShape res = SearchShapeHit(rFromCam);

            //Si on a rencontré une forme
            if (res.coeff != float.MaxValue && res.shape != null)
            {               
                cpt++;
                Vector3 pointOnShape = rFromCam.GetPointAt(res.coeff);
                //On décale i un tout petit peu vers l'extérieur de la forme pour être sur de pas être dans la forme.

                Vector3 normalOnPointOnShape = res.shape.GetNormal(pointOnShape);

                Vector3 pointOnShapeDecal = Vector3.Add(pointOnShape, Vector3.Multiply(normalOnPointOnShape, 0.5f));

                Vector3 indirectLight = new Vector3(0, 0, 0);

                if (cpt < 5)
                {
                    Vector3 newDir;
                   
                    if (res.shape.material.mat == Materials.Mirror)
                    {
                        
                        //On calcule la direction de réflexion
                        newDir = Vector3.Add(
                            Vector3.Multiply(2 * -Vector3.Dot(rFromCam.direction, normalOnPointOnShape), normalOnPointOnShape)
                            , rFromCam.direction);

                        indirectLight = res.shape.material.albedo * IndirectLightning(pointOnShapeDecal, newDir, res, cpt);
                    }
                    else
                    {
                        //On génère un rebond aléatoire
                        newDir = RandomBounce(res, pointOnShapeDecal);

                       // indirectLight = IndirectLightning(pointOnShapeDecal, newDir, res, cpt);

                    }
                }

                if (res.shape.material.mat != Materials.Mirror)
                {
                    Vector3 l = Vector3.Subtract(light.origine, pointOnShapeDecal);
                    float dist = l.Length();
                    l = Vector3.Normalize(l);

                    Vector3 lightEmmited = Vector3.Divide(
                        Vector3.Multiply(res.shape.material.albedo, Math.Max(Math.Min(Vector3.Dot(normalOnPointOnShape, l), 1.0f), 0.0f))
                        , (float)Math.PI);
                    Vector3 directLight = DirectLightning(pointOnShapeDecal, res, dist);

                    return Vector3.Add(Vector3.Multiply(lightEmmited, directLight), indirectLight);
                }
                else
                {
                    return indirectLight;
                }
            }

            return new Vector3(0, 0, 0); //On ne voit rien, on retourne la couleur du fond de l'image  -- JAMAIS UTILISE CHEZ NOUS CAR SCENE COMPLETEMENT REMPLIE
        }

        private Vector3 DirectLightning(Vector3 point, ResFindShape res, float dist)
        {
            //On créé un rayon de la forme jusqu'à la lumière
            Rayon r2 = new Rayon(point, Vector3.Subtract(light.origine, point));
            bool seeTheLight = true;
            foreach (Shape s in shapes)
            {
                float coeff = r2.IntersectAShape(s);


                //Si on croise une forme avant d'arriver à la lumière
                if (coeff != -1 && coeff < 1)
                {
                    seeTheLight = false;
                    break;
                }

            }

            if (seeTheLight)
            {

                Vector3 powerReceived = Vector3.Multiply(light.power, 1 / (dist * dist));

                return powerReceived;  //On voit une forme et elle est éclairé, on retourne sa couleur
            }
            else
            {
                return new Vector3(0, 0, 0); //On voit une forme mais elle n'est pas éclairé, on renvoie du noir
            }
        }

        private Vector3 IndirectLightning(Vector3 point, Vector3 dir, ResFindShape res, int cpt)
        {

            Rayon mirrorRayon = new Rayon(point, dir);
            return Vector3.Multiply(res.shape.material.albedo, SendRayon(mirrorRayon, cpt));
        }

        private Vector3 RandomBounce(ResFindShape res, Vector3 point)
        {

            double r1 = random.NextDouble();
            double r2 = random.NextDouble();
            //On créé une direction aléatoire
            float X = (float)(Math.Cos(2 * Math.PI * r1) * Math.Sqrt(1 - r2));
            float Y = (float)(Math.Sin(2 * Math.PI * r1) * Math.Sqrt(1 - r2));
            float Z = (float)Math.Sqrt(r2);

            Vector3 randomVector = new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());

            Vector3 xBase = Vector3.Normalize(Vector3.Cross(point, randomVector));
            Vector3 yBase = Vector3.Cross(xBase, point);
            Vector3 zBase = point;

            return Vector3.Add(Vector3.Add(X * xBase, Y * yBase), Z * zBase);


        }

        private ResFindShape SearchShapeHit(Rayon rayon)
        {
            ResFindShape res = new ResFindShape();
            res.coeff = float.MaxValue;
            List<Shape> allElements = walls;
            allElements.Add(tree);

            foreach (Shape s in allElements)
            {
                float temp = rayon.IntersectAShape(s);
                if (temp != -1 && temp < res.coeff)
                {
                    res.coeff = temp;
                    if(s is Tree)
                    {
                        res.shape = null;
                    }
                    else
                    {
                        res.shape = s;
                    }
                    res.shape = s;
                }
            }
            return res;
        }
        private struct ResFindShape
        {
            public float coeff;
            public Shape shape;
        }


    }
}