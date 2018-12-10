using System.Collections.Generic;
using OpenCVForUnity;
using OpenCVForUnityExample;
using UnityEngine;
using static OpenCVForUnity.Imgproc;
using static OpenCVForUnity.Core;

[RequireComponent(typeof(WebCamTextureToMatHelper), typeof(FpsMonitor))]
public class DetectHand : MonoBehaviour 
{
    Texture2D _texture;
    WebCamTexture _webcamtex;
    WebCamTextureToMatHelper _webCamTextureToMatHelper;
    FpsMonitor _fpsMonitor;
    Mat _rgbMat, _hand, _handMask, _label, _contour;
    
    static readonly Scalar SKIN_LOWER = new Scalar(0, 70, 90);
    static readonly Scalar SKIN_UPPER = new Scalar(35, 255, 255);
    
    void Start()
    {
        _fpsMonitor = GetComponent<FpsMonitor>();
        _webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();
        _webCamTextureToMatHelper.Initialize();
    }
    
    public void OnWebCamTextureToMatHelperInitialized ()
    {
        Debug.Log ("OnWebCamTextureToMatHelperInitialized");

        var webCamTextureMat = _webCamTextureToMatHelper.GetMat();
        _texture = new Texture2D (webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
        gameObject.GetComponent<Renderer>().material.mainTexture = _texture;
        
        gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols(), webCamTextureMat.rows(), 1);
        Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

        if (_fpsMonitor != null){
            _fpsMonitor.Add ("width", webCamTextureMat.width().ToString());
            _fpsMonitor.Add ("height", webCamTextureMat.height().ToString());
            _fpsMonitor.Add ("orientation", Screen.orientation.ToString());
        }
        
        float width = webCamTextureMat.width ();
        float height = webCamTextureMat.height ();
                                
        float widthScale = Screen.width / width;
        float heightScale = Screen.height / height;
        if (widthScale < heightScale) {
            Camera.main.orthographicSize = (width * Screen.height / Screen.width) / 2;
        } else {
            Camera.main.orthographicSize = height / 2;
        }
        
        _rgbMat = new Mat (webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC3);
        _hand = new Mat (webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC4);
        _handMask = new Mat (webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC1);
        _label = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_32S);
        _contour = new Mat (webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC3);
    }

    public void OnWebCamTextureToMatHelperDisposed ()
    {
        Debug.Log ("OnWebCamTextureToMatHelperDisposed");
        if (_texture != null) {
            Destroy(_texture);
            _texture = null;
        }
    }
    
    void Update()
    {
        if (!_webCamTextureToMatHelper.IsPlaying() || !_webCamTextureToMatHelper.DidUpdateThisFrame()) return;
        
        var rgbaMat = _webCamTextureToMatHelper.GetMat();
        Detect(rgbaMat);
        Utils.fastMatToTexture2D(_hand, _texture);
    }

    void Detect(Mat rgbaMat)
    {
        //RGBAをHSVに変換
        cvtColor(rgbaMat, _rgbMat, COLOR_RGBA2RGB);
        cvtColor(_rgbMat, _rgbMat, COLOR_RGB2HSV);
        
        //肌色領域を抽出
        inRange(_rgbMat, SKIN_LOWER, SKIN_UPPER, _handMask);
        morphologyEx(_handMask, _handMask, MORPH_OPEN, new Mat(), new Point(-1, -1), 3);
        morphologyEx(_handMask, _handMask, MORPH_CLOSE, new Mat(), new Point(-1, -1), 3);

        //ラベリングして重心を取得
        var centroids = new Mat();
        var stats = new Mat();
        var nLabels = connectedComponentsWithStats(_handMask, _label, stats, centroids);

        //最大の領域の重心を取得
        var maxAreaIndex = 0;
        var maxArea = 0.0;
        for (int i = 1; i < nLabels; i++) {    //0番目は背景のため飛ばす
            var area = stats.get(i, CC_STAT_AREA);
            if (area[0] > maxArea) {
                maxArea = area[0];
                maxAreaIndex = i;
            }
        }
        var maxAreaCentroid = new Point(centroids.row(maxAreaIndex).get(0, 0));
//        var maxAreaCentroid = centroids.row(maxAreaIndex).get(0, 0);
        print(maxAreaCentroid);

//        var contours = new List<MatOfPoint>();
//        findContours(_handMask, contours, new Mat(), RETR_EXTERNAL, CHAIN_APPROX_SIMPLE);
//        var contour = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
//        drawContours(contour, contours, -1, new Scalar(255, 0, 0));
//        Debug.Log(contours.Count);
        
        cvtColor(_handMask, _rgbMat, COLOR_GRAY2RGB);
        cvtColor(_rgbMat, _hand, COLOR_RGB2RGBA);
    }
    
    void OnDestroy()
    {
        _webCamTextureToMatHelper.Dispose ();
    }
}
