using UnityEngine;

// SuperResolution v0.1 by MacGruber.
// Make the VaM internal screenshot tool do screenshots in 8K resolution.

namespace MacGruber
{	
	public class SuperResolution : MVRScript
	{	
		RenderTexture oriTexture;
		RenderTexture hqTexture;

		private void OnEnable()
		{
			Camera screenshotCamera = SuperController.singleton.hiResScreenshotCamera;
			if (oriTexture == null)
				oriTexture = screenshotCamera.targetTexture;
			RenderTextureDescriptor descriptor = oriTexture.descriptor;
			descriptor.width = 8192;
			descriptor.height = 4608;
			hqTexture = new RenderTexture(descriptor);
			hqTexture.Create();
			screenshotCamera.targetTexture = hqTexture;
			
			Renderer renderer = SuperController.singleton.hiResScreenshotPreview.GetComponent<Renderer>();
			renderer.sharedMaterial.mainTexture = hqTexture;
		}
		
		private void OnDisable()
		{
			if (oriTexture != null)
			{
				Camera screenshotCamera = SuperController.singleton.hiResScreenshotCamera;
				screenshotCamera.targetTexture = oriTexture;
				
				Renderer renderer = SuperController.singleton.hiResScreenshotPreview.GetComponent<Renderer>();
				renderer.sharedMaterial.mainTexture = oriTexture;
				
				oriTexture = null;
			}
			if (hqTexture != null)
			{
				hqTexture.Release();
				Destroy(hqTexture);
				hqTexture = null;
			}
		}
	}
}