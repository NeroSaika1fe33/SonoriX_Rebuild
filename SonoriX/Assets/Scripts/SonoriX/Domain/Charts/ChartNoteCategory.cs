namespace SonoriX.Domain.Charts
{
    //基本Note种类
    public enum ChartNoteCategory
    {
        //中央4key单点Note
        FixedTap,
        //中央4key长按Note
        FixedLong,
        //位置自由长度自由的单点Note
        FreeTap,
        //位置自由长度自由的长安Note
        FreeLong,
        //根据滑条位置的左右移动反应的Note
        SliderFlick,
        //根据滑条位置反应的Note
        SliderLong,
        //根据Volume旋钮的左旋右旋反应的Note
        VolumeFlick,
        //根据Volume旋钮持续左旋右旋反应的长条型Note
        VolumeLong,
        //用于演示做反应的长条型Note
        Guide
    }
}