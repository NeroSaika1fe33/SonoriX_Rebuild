using UnityEngine;

namespace FFrameWork
{
	public static class ShaderPropertyID
	{
		// タップエフェクト用 Projection 行列の Shader Property ID
		public static readonly int TAP_EFFECT_PROJECTION_MATRIX_ID = Shader.PropertyToID("_TapEffectProjectionMatrix");

		// タップエフェクト用 View 行列の Shader Property ID
		public static readonly int TAP_EFFECT_VIEW_MATRIX_ID = Shader.PropertyToID("_TapEffectViewMatrix");
	}
}
