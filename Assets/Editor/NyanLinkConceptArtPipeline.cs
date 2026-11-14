using System.Collections.Generic;
using System.IO;
using NineMood.Presentation;
using UnityEditor;
using UnityEngine;

public static class NyanLinkConceptArtPipeline
{
    const string Root="Assets/Resources/Art/ConceptPixel/";
    public static void Prepare()
    {
        var records=new List<ConceptSpriteRecord>();
        Add(records,"cat-sheet","cat-",2,2,1.45f,false,FilterMode.Point);
        Add(records,"rat-sheet","boss-",2,1,2.10f,false,FilterMode.Point);
        Add(records,"obstacle-sheet","obstacle-",3,1,1.5f,false,FilterMode.Point);
        Add(records,"tile-sheet","tile-",3,2,1f,true,FilterMode.Bilinear);
        Add(records,"title-logo","logo-",1,1,1f,false,FilterMode.Point);
        Add(records,"city-loop","city-",1,1,11f,false,FilterMode.Point,true);
        File.WriteAllText(Root+"catalog.json",JsonUtility.ToJson(new ConceptSpriteCatalog{entries=records.ToArray()},true));
        AssetDatabase.ImportAsset(Root+"catalog.json");
        foreach(var path in Directory.GetFiles("Assets/Resources/Audio/ConceptPixel","*.wav"))
        {
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;
            settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.preloadAudioData=true;settings.compressionFormat=AudioCompressionFormat.PCM;
            importer.defaultSampleSettings=settings;importer.forceToMono=false;importer.SaveAndReimport();
        }
        Debug.Log("Concept art catalog prepared: "+records.Count+" sprites; CPU texture copies disabled.");
    }
    static void Add(List<ConceptSpriteRecord> records,string texture,string prefix,int cols,int rows,float height,bool square,FilterMode filter,bool opaque=false)
    {
        var path=Root+texture+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;importer.isReadable=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=!opaque;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.filterMode=filter;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=tex.GetPixels32();int cw=tex.width/cols,ch=tex.height/rows;
        var bounds=new List<RectInt>();
        for(int i=0;i<cols*rows;i++)
        {
            int x0=(i%cols)*cw,y0=(rows-1-i/cols)*ch,xmin=x0+cw,ymin=y0+ch,xmax=x0,ymax=y0;
            if(opaque){xmin=x0;ymin=y0;xmax=x0+cw-1;ymax=y0+ch-1;}
            else for(int y=y0;y<y0+ch;y++)for(int x=x0;x<x0+cw;x++)if(pixels[y*tex.width+x].a>220)
            {xmin=Mathf.Min(xmin,x);ymin=Mathf.Min(ymin,y);xmax=Mathf.Max(xmax,x);ymax=Mathf.Max(ymax,y);}
            if(xmax<xmin)throw new System.Exception("Empty atlas cell: "+prefix+i);
            if(square)
            {
                int side=Mathf.Min(Mathf.Max(xmax-xmin+3,ymax-ymin+3),Mathf.Min(cw,ch));
                xmin=Mathf.Clamp((xmin+xmax+1-side)/2,x0,x0+cw-side);ymin=Mathf.Clamp((ymin+ymax+1-side)/2,y0,y0+ch-side);xmax=xmin+side-1;ymax=ymin+side-1;
            }
            bounds.Add(new RectInt(xmin,ymin,xmax-xmin+1,ymax-ymin+1));
        }
        float ppu=0;
        if(!square)for(int i=0;i<(prefix=="cat-" ? 2:prefix=="boss-" ? 1:bounds.Count);i++)ppu=Mathf.Max(ppu,bounds[i].height/height);
        for(int i=0;i<bounds.Count;i++)
        {
            var b=bounds[i];float unit= square ? b.width : ppu;
            if(prefix=="obstacle-")unit=b.height/(i==0 ? .65f:i==1 ? 1.30f:1.45f);
            records.Add(new ConceptSpriteRecord{id=prefix+i,texture=texture,x=b.x,y=b.y,width=b.width,height=b.height,ppu=unit,pivotX=.5f,pivotY=(prefix=="cat-" || prefix=="boss-" || prefix=="obstacle-") ? 0f:.5f});
        }
        importer.isReadable=false;importer.SaveAndReimport();
    }
}
