using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UniRx;

public class ComparisonWindow : MonoBehaviour
{
    [SerializeField] Image _LoadedImage = default;
    [SerializeField] Image _SimilarImage = default;
    [SerializeField] AspectRatioFitter _LoadedImageFitter = default;
    [SerializeField] AspectRatioFitter _SimilarImageFitter = default;
    [SerializeField] Button _SkipButton = default;
    [SerializeField] Button _ContinueButton = default;
    Subject<bool> ClickSubject = new Subject<bool>();
    public IObservable<bool> OnClick => ClickSubject;

    private void Awake()
    {
        _SkipButton.OnClickAsObservable()
            .Subscribe(_ => ClickSubject.OnNext(false)).AddTo(this);
        _ContinueButton.OnClickAsObservable()
            .Subscribe(_ => ClickSubject.OnNext(true)).AddTo(this);
    }
    public void SetSprites(Sprite loadedSprite, Sprite similarSprite)
    {
        var loadedSize = loadedSprite.rect.size;
        var similarSize = similarSprite.rect.size;
        _LoadedImageFitter.aspectRatio = loadedSize.x / loadedSize.y;
        _SimilarImageFitter.aspectRatio = similarSize.x / similarSize.y;
        _LoadedImage.sprite = loadedSprite;
        _SimilarImage.sprite = similarSprite;
    }
    public void SetEnable(bool enable) => gameObject.SetActive(enable);

}
