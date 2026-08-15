using System;
using System.Text;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Helper class for drawing text strings and sprites in one or more optimized batches.
/// </summary>
public class SpriteBatch : GraphicsResource
{
	private readonly SpriteBatcher _batcher;

	private SpriteSortMode _sortMode;

	private BlendState _blendState;

	private SamplerState _samplerState;

	private DepthStencilState _depthStencilState;

	private RasterizerState _rasterizerState;

	private Effect _effect;

	private bool _beginCalled;

	private SpriteEffect _spriteEffect;

	private readonly EffectPass _spritePass;

	private Rectangle _tempRect = new Rectangle(0, 0, 0, 0);

	private Vector2 _texCoordTL = new Vector2(0f, 0f);

	private Vector2 _texCoordBR = new Vector2(0f, 0f);

	/// <summary>
	/// The amount of texels to tuck in the texture in order to avoid artifacts.
	/// </summary>
	public static float TextureTuckAmount;

	public static Matrix? globalMatrix;

	private void _TuckTextureCoordinates(Texture2D texture, ref Vector2 tl, ref Vector2 br)
	{
		tl.X += TextureTuckAmount * texture.TexelWidth;
		br.X -= TextureTuckAmount * texture.TexelWidth;
		tl.Y += TextureTuckAmount * texture.TexelHeight;
		br.Y -= TextureTuckAmount * texture.TexelHeight;
	}

	/// <summary>
	/// Constructs a <see cref="T:Microsoft.Xna.Framework.Graphics.SpriteBatch" />.
	/// </summary>
	/// <param name="graphicsDevice">The <see cref="T:Microsoft.Xna.Framework.Graphics.GraphicsDevice" />, which will be used for sprite rendering.</param>        
	/// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="graphicsDevice" /> is null.</exception>
	public SpriteBatch(GraphicsDevice graphicsDevice)
		: this(graphicsDevice, 0)
	{
	}

	/// <summary>
	/// Constructs a <see cref="T:Microsoft.Xna.Framework.Graphics.SpriteBatch" />.
	/// </summary>
	/// <param name="graphicsDevice">The <see cref="T:Microsoft.Xna.Framework.Graphics.GraphicsDevice" />, which will be used for sprite rendering.</param>
	/// <param name="capacity">The initial capacity of the internal array holding batch items (the value will be rounded to the next multiple of 64).</param>
	/// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="graphicsDevice" /> is null.</exception>
	public SpriteBatch(GraphicsDevice graphicsDevice, int capacity)
	{
		if (graphicsDevice == null)
		{
			throw new ArgumentNullException("graphicsDevice", "The GraphicsDevice must not be null when creating new resources.");
		}
		base.GraphicsDevice = graphicsDevice;
		_spriteEffect = new SpriteEffect(graphicsDevice);
		_spritePass = _spriteEffect.CurrentTechnique.Passes[0];
		_batcher = new SpriteBatcher(graphicsDevice, capacity);
		_beginCalled = false;
	}

	/// <summary>
	/// Begins a new sprite and text batch with the specified render state.
	/// </summary>
	/// <param name="sortMode">The drawing order for sprite and text drawing. <see cref="F:Microsoft.Xna.Framework.Graphics.SpriteSortMode.Deferred" /> by default.</param>
	/// <param name="blendState">State of the blending. Uses <see cref="F:Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend" /> if null.</param>
	/// <param name="samplerState">State of the sampler. Uses <see cref="F:Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp" /> if null.</param>
	/// <param name="depthStencilState">State of the depth-stencil buffer. Uses <see cref="F:Microsoft.Xna.Framework.Graphics.DepthStencilState.None" /> if null.</param>
	/// <param name="rasterizerState">State of the rasterization. Uses <see cref="F:Microsoft.Xna.Framework.Graphics.RasterizerState.CullCounterClockwise" /> if null.</param>
	/// <param name="effect">A custom <see cref="T:Microsoft.Xna.Framework.Graphics.Effect" /> to override the default sprite effect. Uses default sprite effect if null.</param>
	/// <param name="transformMatrix">An optional matrix used to transform the sprite geometry. Uses <see cref="P:Microsoft.Xna.Framework.Matrix.Identity" /> if null.</param>
	/// <exception cref="T:System.InvalidOperationException">Thrown if <see cref="M:Microsoft.Xna.Framework.Graphics.SpriteBatch.Begin(Microsoft.Xna.Framework.Graphics.SpriteSortMode,Microsoft.Xna.Framework.Graphics.BlendState,Microsoft.Xna.Framework.Graphics.SamplerState,Microsoft.Xna.Framework.Graphics.DepthStencilState,Microsoft.Xna.Framework.Graphics.RasterizerState,Microsoft.Xna.Framework.Graphics.Effect,System.Nullable{Microsoft.Xna.Framework.Matrix})" /> is called next time without previous <see cref="M:Microsoft.Xna.Framework.Graphics.SpriteBatch.End" />.</exception>
	/// <remarks>This method uses optional parameters.</remarks>
	/// <remarks>The <see cref="M:Microsoft.Xna.Framework.Graphics.SpriteBatch.Begin(Microsoft.Xna.Framework.Graphics.SpriteSortMode,Microsoft.Xna.Framework.Graphics.BlendState,Microsoft.Xna.Framework.Graphics.SamplerState,Microsoft.Xna.Framework.Graphics.DepthStencilState,Microsoft.Xna.Framework.Graphics.RasterizerState,Microsoft.Xna.Framework.Graphics.Effect,System.Nullable{Microsoft.Xna.Framework.Matrix})" /> Begin should be called before drawing commands, and you cannot call it again before subsequent <see cref="M:Microsoft.Xna.Framework.Graphics.SpriteBatch.End" />.</remarks>
	public void Begin(SpriteSortMode sortMode = SpriteSortMode.Deferred, BlendState blendState = null, SamplerState samplerState = null, DepthStencilState depthStencilState = null, RasterizerState rasterizerState = null, Effect effect = null, Matrix? transformMatrix = null)
	{
		if (_beginCalled)
		{
			throw new InvalidOperationException("Begin cannot be called again until End has been successfully called.");
		}
		_sortMode = sortMode;
		_blendState = blendState ?? BlendState.AlphaBlend;
		_samplerState = samplerState ?? SamplerState.LinearClamp;
		_depthStencilState = depthStencilState ?? DepthStencilState.None;
		_rasterizerState = rasterizerState ?? RasterizerState.CullCounterClockwise;
		_effect = effect;
		_spriteEffect.TransformMatrix = transformMatrix;
		if (globalMatrix.HasValue)
		{
			if (!_spriteEffect.TransformMatrix.HasValue)
			{
				_spriteEffect.TransformMatrix = globalMatrix;
			}
			else
			{
				_spriteEffect.TransformMatrix = globalMatrix * _spriteEffect.TransformMatrix;
			}
		}
		if (sortMode == SpriteSortMode.Immediate)
		{
			Setup();
		}
		_beginCalled = true;
	}

	/// <summary>
	/// Flushes all batched text and sprites to the screen.
	/// </summary>
	/// <remarks>This command should be called after <see cref="M:Microsoft.Xna.Framework.Graphics.SpriteBatch.Begin(Microsoft.Xna.Framework.Graphics.SpriteSortMode,Microsoft.Xna.Framework.Graphics.BlendState,Microsoft.Xna.Framework.Graphics.SamplerState,Microsoft.Xna.Framework.Graphics.DepthStencilState,Microsoft.Xna.Framework.Graphics.RasterizerState,Microsoft.Xna.Framework.Graphics.Effect,System.Nullable{Microsoft.Xna.Framework.Matrix})" /> and drawing commands.</remarks>
	public void End()
	{
		if (!_beginCalled)
		{
			throw new InvalidOperationException("Begin must be called before calling End.");
		}
		_beginCalled = false;
		if (_sortMode != SpriteSortMode.Immediate)
		{
			Setup();
		}
		_batcher.DrawBatch(_sortMode, _effect);
	}

	private void Setup()
	{
		GraphicsDevice obj = base.GraphicsDevice;
		obj.BlendState = _blendState;
		obj.DepthStencilState = _depthStencilState;
		obj.RasterizerState = _rasterizerState;
		obj.SamplerStates[0] = _samplerState;
		_spritePass.Apply();
	}

	private void CheckValid(Texture2D texture)
	{
		if (texture == null)
		{
			throw new ArgumentNullException("texture");
		}
		if (!_beginCalled)
		{
			throw new InvalidOperationException("Draw was called, but Begin has not yet been called. Begin must be called successfully before you can call Draw.");
		}
		if (texture.IsDisposed)
		{
			throw new ObjectDisposedException("Can't draw texture" + ((texture.Name != null) ? (" '" + texture.Name + "'") : "") + " because it's disposed.");
		}
	}

	private void CheckValid(SpriteFont spriteFont, string text)
	{
		if (spriteFont == null)
		{
			throw new ArgumentNullException("spriteFont");
		}
		if (text == null)
		{
			throw new ArgumentNullException("text");
		}
		if (!_beginCalled)
		{
			throw new InvalidOperationException("DrawString was called, but Begin has not yet been called. Begin must be called successfully before you can call DrawString.");
		}
	}

	private void CheckValid(SpriteFont spriteFont, StringBuilder text)
	{
		if (spriteFont == null)
		{
			throw new ArgumentNullException("spriteFont");
		}
		if (text == null)
		{
			throw new ArgumentNullException("text");
		}
		if (!_beginCalled)
		{
			throw new InvalidOperationException("DrawString was called, but Begin has not yet been called. Begin must be called successfully before you can call DrawString.");
		}
	}

	/// <summary>
	/// Submit a sprite for drawing in the current batch.
	/// </summary>
	/// <param name="texture">A texture.</param>
	/// <param name="position">The drawing location on screen.</param>
	/// <param name="sourceRectangle">An optional region on the texture which will be rendered. If null - draws full texture.</param>
	/// <param name="color">A color mask.</param>
	/// <param name="rotation">A rotation of this sprite.</param>
	/// <param name="origin">Center of the rotation. 0,0 by default.</param>
	/// <param name="scale">A scaling of this sprite.</param>
	/// <param name="effects">Modificators for drawing. Can be combined.</param>
	/// <param name="layerDepth">A depth of the layer of this sprite.</param>
	public void Draw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
	{
		CheckValid(texture);
		SpriteBatchItem spriteBatchItem = _batcher.CreateBatchItem();
		spriteBatchItem.Texture = texture;
		switch (_sortMode)
		{
		case SpriteSortMode.Texture:
			spriteBatchItem.SortKey = texture.SortingKey;
			break;
		case SpriteSortMode.FrontToBack:
			spriteBatchItem.SortKey = layerDepth;
			break;
		case SpriteSortMode.BackToFront:
			spriteBatchItem.SortKey = 0f - layerDepth;
			break;
		}
		origin *= scale;
		float w;
		float h;
		if (sourceRectangle.HasValue)
		{
			Rectangle valueOrDefault = sourceRectangle.GetValueOrDefault();
			w = (float)valueOrDefault.Width * scale.X;
			h = (float)valueOrDefault.Height * scale.Y;
			_texCoordTL.X = (float)valueOrDefault.X * texture.TexelWidth;
			_texCoordTL.Y = (float)valueOrDefault.Y * texture.TexelHeight;
			_texCoordBR.X = (float)(valueOrDefault.X + valueOrDefault.Width) * texture.TexelWidth;
			_texCoordBR.Y = (float)(valueOrDefault.Y + valueOrDefault.Height) * texture.TexelHeight;
		}
		else
		{
			w = (float)texture.Width * scale.X;
			h = (float)texture.Height * scale.Y;
			_texCoordTL = Vector2.Zero;
			_texCoordBR.X = (float)texture.width * texture.TexelWidth;
			_texCoordBR.Y = (float)texture.height * texture.TexelHeight;
		}
		_TuckTextureCoordinates(texture, ref _texCoordTL, ref _texCoordBR);
		if ((effects & SpriteEffects.FlipVertically) != SpriteEffects.None)
		{
			float y = _texCoordBR.Y;
			_texCoordBR.Y = _texCoordTL.Y;
			_texCoordTL.Y = y;
		}
		if ((effects & SpriteEffects.FlipHorizontally) != SpriteEffects.None)
		{
			float x = _texCoordBR.X;
			_texCoordBR.X = _texCoordTL.X;
			_texCoordTL.X = x;
		}
		if (rotation == 0f)
		{
			spriteBatchItem.Set(position.X - origin.X, position.Y - origin.Y, w, h, color, _texCoordTL, _texCoordBR, layerDepth);
		}
		else
		{
			spriteBatchItem.Set(position.X, position.Y, 0f - origin.X, 0f - origin.Y, w, h, (float)Math.Sin(rotation), (float)Math.Cos(rotation), color, _texCoordTL, _texCoordBR, layerDepth);
		}
		FlushIfNeeded();
	}

	/// <summary>
	/// Submit a sprite for drawing in the current batch.
	/// </summary>
	/// <param name="texture">A texture.</param>
	/// <param name="position">The drawing location on screen.</param>
	/// <param name="sourceRectangle">An optional region on the texture which will be rendered. If null - draws full texture.</param>
	/// <param name="color">A color mask.</param>
	/// <param name="rotation">A rotation of this sprite.</param>
	/// <param name="origin">Center of the rotation. 0,0 by default.</param>
	/// <param name="scale">A scaling of this sprite.</param>
	/// <param name="effects">Modificators for drawing. Can be combined.</param>
	/// <param name="layerDepth">A depth of the layer of this sprite.</param>
	public void Draw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
	{
		Vector2 scale2 = new Vector2(scale, scale);
		Draw(texture, position, sourceRectangle, color, rotation, origin, scale2, effects, layerDepth);
	}

	/// <summary>
	/// Submit a sprite for drawing in the current batch.
	/// </summary>
	/// <param name="texture">A texture.</param>
	/// <param name="destinationRectangle">The drawing bounds on screen.</param>
	/// <param name="sourceRectangle">An optional region on the texture which will be rendered. If null - draws full texture.</param>
	/// <param name="color">A color mask.</param>
	/// <param name="rotation">A rotation of this sprite.</param>
	/// <param name="origin">Center of the rotation. 0,0 by default.</param>
	/// <param name="effects">Modificators for drawing. Can be combined.</param>
	/// <param name="layerDepth">A depth of the layer of this sprite.</param>
	public void Draw(Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, SpriteEffects effects, float layerDepth)
	{
		CheckValid(texture);
		SpriteBatchItem spriteBatchItem = _batcher.CreateBatchItem();
		spriteBatchItem.Texture = texture;
		switch (_sortMode)
		{
		case SpriteSortMode.Texture:
			spriteBatchItem.SortKey = texture.SortingKey;
			break;
		case SpriteSortMode.FrontToBack:
			spriteBatchItem.SortKey = layerDepth;
			break;
		case SpriteSortMode.BackToFront:
			spriteBatchItem.SortKey = 0f - layerDepth;
			break;
		}
		if (sourceRectangle.HasValue)
		{
			Rectangle valueOrDefault = sourceRectangle.GetValueOrDefault();
			_texCoordTL.X = (float)valueOrDefault.X * texture.TexelWidth;
			_texCoordTL.Y = (float)valueOrDefault.Y * texture.TexelHeight;
			_texCoordBR.X = (float)(valueOrDefault.X + valueOrDefault.Width) * texture.TexelWidth;
			_texCoordBR.Y = (float)(valueOrDefault.Y + valueOrDefault.Height) * texture.TexelHeight;
			if (valueOrDefault.Width != 0)
			{
				origin.X = origin.X * (float)destinationRectangle.Width / (float)valueOrDefault.Width;
			}
			else
			{
				origin.X = origin.X * (float)destinationRectangle.Width * texture.TexelWidth;
			}
			if (valueOrDefault.Height != 0)
			{
				origin.Y = origin.Y * (float)destinationRectangle.Height / (float)valueOrDefault.Height;
			}
			else
			{
				origin.Y = origin.Y * (float)destinationRectangle.Height * texture.TexelHeight;
			}
		}
		else
		{
			_texCoordTL = Vector2.Zero;
			_texCoordBR.X = (float)texture.width * texture.TexelWidth;
			_texCoordBR.Y = (float)texture.height * texture.TexelHeight;
			origin.X = origin.X * (float)destinationRectangle.Width * texture.TexelWidth;
			origin.Y = origin.Y * (float)destinationRectangle.Height * texture.TexelHeight;
		}
		_TuckTextureCoordinates(texture, ref _texCoordTL, ref _texCoordBR);
		if ((effects & SpriteEffects.FlipVertically) != SpriteEffects.None)
		{
			float y = _texCoordBR.Y;
			_texCoordBR.Y = _texCoordTL.Y;
			_texCoordTL.Y = y;
		}
		if ((effects & SpriteEffects.FlipHorizontally) != SpriteEffects.None)
		{
			float x = _texCoordBR.X;
			_texCoordBR.X = _texCoordTL.X;
			_texCoordTL.X = x;
		}
		if (rotation == 0f)
		{
			spriteBatchItem.Set((float)destinationRectangle.X - origin.X, (float)destinationRectangle.Y - origin.Y, destinationRectangle.Width, destinationRectangle.Height, color, _texCoordTL, _texCoordBR, layerDepth);
		}
		else
		{
			spriteBatchItem.Set(destinationRectangle.X, destinationRectangle.Y, 0f - origin.X, 0f - origin.Y, destinationRectangle.Width, destinationRectangle.Height, (float)Math.Sin(rotation), (float)Math.Cos(rotation), color, _texCoordTL, _texCoordBR, layerDepth);
		}
		FlushIfNeeded();
	}

	internal void FlushIfNeeded()
	{
		if (_sortMode == SpriteSortMode.Immediate)
		{
			_batcher.DrawBatch(_sortMode, _effect);
		}
	}

	/// <summary>
	/// Submit a sprite for drawing in the current batch.
	/// </summary>
	/// <param name="texture">A texture.</param>
	/// <param name="position">The drawing location on screen.</param>
	/// <param name="sourceRectangle">An optional region on the texture which will be rendered. If null - draws full texture.</param>
	/// <param name="color">A color mask.</param>
	public void Draw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color)
	{
		CheckValid(texture);
		SpriteBatchItem spriteBatchItem = _batcher.CreateBatchItem();
		spriteBatchItem.Texture = texture;
		spriteBatchItem.SortKey = ((_sortMode == SpriteSortMode.Texture) ? texture.SortingKey : 0);
		Vector2 vector;
		if (sourceRectangle.HasValue)
		{
			Rectangle valueOrDefault = sourceRectangle.GetValueOrDefault();
			vector = new Vector2(valueOrDefault.Width, valueOrDefault.Height);
			_texCoordTL.X = (float)valueOrDefault.X * texture.TexelWidth;
			_texCoordTL.Y = (float)valueOrDefault.Y * texture.TexelHeight;
			_texCoordBR.X = (float)(valueOrDefault.X + valueOrDefault.Width) * texture.TexelWidth;
			_texCoordBR.Y = (float)(valueOrDefault.Y + valueOrDefault.Height) * texture.TexelHeight;
		}
		else
		{
			vector = new Vector2(texture.width, texture.height);
			_texCoordTL = Vector2.Zero;
			_texCoordBR.X = (float)texture.width * texture.TexelWidth;
			_texCoordBR.Y = (float)texture.height * texture.TexelHeight;
		}
		_TuckTextureCoordinates(texture, ref _texCoordTL, ref _texCoordBR);
		spriteBatchItem.Set(position.X, position.Y, vector.X, vector.Y, color, _texCoordTL, _texCoordBR, 0f);
		FlushIfNeeded();
	}

	/// <summary>
	/// Submit a sprite for drawing in the current batch.
	/// </summary>
	/// <param name="texture">A texture.</param>
	/// <param name="destinationRectangle">The drawing bounds on screen.</param>
	/// <param name="sourceRectangle">An optional region on the texture which will be rendered. If null - draws full texture.</param>
	/// <param name="color">A color mask.</param>
	public void Draw(Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color)
	{
		CheckValid(texture);
		SpriteBatchItem spriteBatchItem = _batcher.CreateBatchItem();
		spriteBatchItem.Texture = texture;
		spriteBatchItem.SortKey = ((_sortMode == SpriteSortMode.Texture) ? texture.SortingKey : 0);
		if (sourceRectangle.HasValue)
		{
			Rectangle valueOrDefault = sourceRectangle.GetValueOrDefault();
			_texCoordTL.X = (float)valueOrDefault.X * texture.TexelWidth;
			_texCoordTL.Y = (float)valueOrDefault.Y * texture.TexelHeight;
			_texCoordBR.X = (float)(valueOrDefault.X + valueOrDefault.Width) * texture.TexelWidth;
			_texCoordBR.Y = (float)(valueOrDefault.Y + valueOrDefault.Height) * texture.TexelHeight;
		}
		else
		{
			_texCoordTL = Vector2.Zero;
			_texCoordBR.X = (float)texture.width * texture.TexelWidth;
			_texCoordBR.Y = (float)texture.height * texture.TexelHeight;
		}
		_TuckTextureCoordinates(texture, ref _texCoordTL, ref _texCoordBR);
		spriteBatchItem.Set(destinationRectangle.X, destinationRectangle.Y, destinationRectangle.Width, destinationRectangle.Height, color, _texCoordTL, _texCoordBR, 0f);
		FlushIfNeeded();
	}

	/// <summary>
	/// Submit a sprite for drawing in the current batch.
	/// </summary>
	/// <param name="texture">A texture.</param>
	/// <param name="position">The drawing location on screen.</param>
	/// <param name="color">A color mask.</param>
	public void Draw(Texture2D texture, Vector2 position, Color color)
	{
		CheckValid(texture);
		SpriteBatchItem spriteBatchItem = _batcher.CreateBatchItem();
		spriteBatchItem.Texture = texture;
		spriteBatchItem.SortKey = ((_sortMode == SpriteSortMode.Texture) ? texture.SortingKey : 0);
		spriteBatchItem.Set(position.X, position.Y, texture.Width, texture.Height, color, Vector2.Zero, new Vector2((float)texture.width * texture.TexelWidth, (float)texture.height * texture.TexelHeight), 0f);
		FlushIfNeeded();
	}

	/// <summary>
	/// Submit a sprite for drawing in the current batch.
	/// </summary>
	/// <param name="texture">A texture.</param>
	/// <param name="destinationRectangle">The drawing bounds on screen.</param>
	/// <param name="color">A color mask.</param>
	public void Draw(Texture2D texture, Rectangle destinationRectangle, Color color)
	{
		CheckValid(texture);
		SpriteBatchItem spriteBatchItem = _batcher.CreateBatchItem();
		spriteBatchItem.Texture = texture;
		spriteBatchItem.SortKey = ((_sortMode == SpriteSortMode.Texture) ? texture.SortingKey : 0);
		spriteBatchItem.Set(destinationRectangle.X, destinationRectangle.Y, destinationRectangle.Width, destinationRectangle.Height, color, Vector2.Zero, new Vector2((float)texture.width * texture.TexelWidth, (float)texture.height * texture.TexelHeight), 0f);
		FlushIfNeeded();
	}

	/// <summary>
	/// Submit a text string of sprites for drawing in the current batch.
	/// </summary>
	/// <param name="spriteFont">A font.</param>
	/// <param name="text">The text which will be drawn.</param>
	/// <param name="position">The drawing location on screen.</param>
	/// <param name="color">A color mask.</param>
	public unsafe void DrawString(SpriteFont spriteFont, string text, Vector2 position, Color color)
	{
		CheckValid(spriteFont, text);
		position = new Vector2((int)position.X, (int)position.Y);
		float sortKey = ((_sortMode == SpriteSortMode.Texture) ? spriteFont.Texture.SortingKey : 0);
		Vector2 zero = Vector2.Zero;
		bool flag = true;
		fixed (SpriteFont.Glyph* glyphs = spriteFont.Glyphs)
		{
			foreach (char c in text)
			{
				switch (c)
				{
				case '\n':
					zero.X = 0f;
					zero.Y += spriteFont.LineSpacing;
					flag = true;
					continue;
				case '\r':
					continue;
				}
				int glyphIndexOrDefault = spriteFont.GetGlyphIndexOrDefault(c);
				SpriteFont.Glyph* ptr = glyphs + glyphIndexOrDefault;
				if (flag)
				{
					zero.X = Math.Max(ptr->LeftSideBearing, 0f);
					flag = false;
				}
				else
				{
					zero.X += spriteFont.Spacing + ptr->LeftSideBearing;
				}
				Vector2 vector = zero;
				vector.X += ptr->Cropping.X;
				vector.Y += ptr->Cropping.Y;
				vector += position;
				SpriteBatchItem spriteBatchItem = _batcher.CreateBatchItem();
				spriteBatchItem.Texture = spriteFont.Texture;
				spriteBatchItem.SortKey = sortKey;
				_texCoordTL.X = (float)ptr->BoundsInTexture.X * spriteFont.Texture.TexelWidth;
				_texCoordTL.Y = (float)ptr->BoundsInTexture.Y * spriteFont.Texture.TexelHeight;
				_texCoordBR.X = (float)(ptr->BoundsInTexture.X + ptr->BoundsInTexture.Width) * spriteFont.Texture.TexelWidth;
				_texCoordBR.Y = (float)(ptr->BoundsInTexture.Y + ptr->BoundsInTexture.Height) * spriteFont.Texture.TexelHeight;
				_TuckTextureCoordinates(spriteFont.Texture, ref _texCoordTL, ref _texCoordBR);
				spriteBatchItem.Set(vector.X, vector.Y, ptr->BoundsInTexture.Width, ptr->BoundsInTexture.Height, color, _texCoordTL, _texCoordBR, 0f);
				zero.X += ptr->Width + ptr->RightSideBearing;
			}
		}
		FlushIfNeeded();
	}

	/// <summary>
	/// Submit a text string of sprites for drawing in the current batch.
	/// </summary>
	/// <param name="spriteFont">A font.</param>
	/// <param name="text">The text which will be drawn.</param>
	/// <param name="position">The drawing location on screen.</param>
	/// <param name="color">A color mask.</param>
	/// <param name="rotation">A rotation of this string.</param>
	/// <param name="origin">Center of the rotation. 0,0 by default.</param>
	/// <param name="scale">A scaling of this string.</param>
	/// <param name="effects">Modificators for drawing. Can be combined.</param>
	/// <param name="layerDepth">A depth of the layer of this string.</param>
	public void DrawString(SpriteFont spriteFont, string text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
	{
		Vector2 scale2 = new Vector2(scale, scale);
		DrawString(spriteFont, text, position, color, rotation, origin, scale2, effects, layerDepth);
	}

	/// <summary>
	/// Submit a text string of sprites for drawing in the current batch.
	/// </summary>
	/// <param name="spriteFont">A font.</param>
	/// <param name="text">The text which will be drawn.</param>
	/// <param name="position">The drawing location on screen.</param>
	/// <param name="color">A color mask.</param>
	/// <param name="rotation">A rotation of this string.</param>
	/// <param name="origin">Center of the rotation. 0,0 by default.</param>
	/// <param name="scale">A scaling of this string.</param>
	/// <param name="effects">Modificators for drawing. Can be combined.</param>
	/// <param name="layerDepth">A depth of the layer of this string.</param>
	public unsafe void DrawString(SpriteFont spriteFont, string text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
	{
		CheckValid(spriteFont, text);
		position = new Vector2((int)position.X, (int)position.Y);
		float sortKey = 0f;
		switch (_sortMode)
		{
		case SpriteSortMode.Texture:
			sortKey = spriteFont.Texture.SortingKey;
			break;
		case SpriteSortMode.FrontToBack:
			sortKey = layerDepth;
			break;
		case SpriteSortMode.BackToFront:
			sortKey = 0f - layerDepth;
			break;
		}
		Vector2 zero = Vector2.Zero;
		bool flag = (effects & SpriteEffects.FlipVertically) == SpriteEffects.FlipVertically;
		bool flag2 = (effects & SpriteEffects.FlipHorizontally) == SpriteEffects.FlipHorizontally;
		if (flag || flag2)
		{
			SpriteFont.CharacterSource text2 = new SpriteFont.CharacterSource(text);
			spriteFont.MeasureString(ref text2, out var size);
			if (flag2)
			{
				origin.X *= -1f;
				zero.X = 0f - size.X;
			}
			if (flag)
			{
				origin.Y *= -1f;
				zero.Y = (float)spriteFont.LineSpacing - size.Y;
			}
		}
		Matrix matrix = Matrix.Identity;
		float num = 0f;
		float num2 = 0f;
		if (rotation == 0f)
		{
			matrix.M11 = (flag2 ? (0f - scale.X) : scale.X);
			matrix.M22 = (flag ? (0f - scale.Y) : scale.Y);
			matrix.M41 = (zero.X - origin.X) * matrix.M11 + position.X;
			matrix.M42 = (zero.Y - origin.Y) * matrix.M22 + position.Y;
		}
		else
		{
			num = (float)Math.Cos(rotation);
			num2 = (float)Math.Sin(rotation);
			matrix.M11 = (flag2 ? (0f - scale.X) : scale.X) * num;
			matrix.M12 = (flag2 ? (0f - scale.X) : scale.X) * num2;
			matrix.M21 = (flag ? (0f - scale.Y) : scale.Y) * (0f - num2);
			matrix.M22 = (flag ? (0f - scale.Y) : scale.Y) * num;
			matrix.M41 = (zero.X - origin.X) * matrix.M11 + (zero.Y - origin.Y) * matrix.M21 + position.X;
			matrix.M42 = (zero.X - origin.X) * matrix.M12 + (zero.Y - origin.Y) * matrix.M22 + position.Y;
		}
		Vector2 zero2 = Vector2.Zero;
		bool flag3 = true;
		fixed (SpriteFont.Glyph* glyphs = spriteFont.Glyphs)
		{
			foreach (char c in text)
			{
				switch (c)
				{
				case '\n':
					zero2.X = 0f;
					zero2.Y += spriteFont.LineSpacing;
					flag3 = true;
					continue;
				case '\r':
					continue;
				}
				int glyphIndexOrDefault = spriteFont.GetGlyphIndexOrDefault(c);
				SpriteFont.Glyph* ptr = glyphs + glyphIndexOrDefault;
				if (flag3)
				{
					zero2.X = Math.Max(ptr->LeftSideBearing, 0f);
					flag3 = false;
				}
				else
				{
					zero2.X += spriteFont.Spacing + ptr->LeftSideBearing;
				}
				Vector2 position2 = zero2;
				if (flag2)
				{
					position2.X += ptr->BoundsInTexture.Width;
				}
				position2.X += ptr->Cropping.X;
				if (flag)
				{
					position2.Y += ptr->BoundsInTexture.Height - spriteFont.LineSpacing;
				}
				position2.Y += ptr->Cropping.Y;
				Vector2.Transform(ref position2, ref matrix, out position2);
				SpriteBatchItem spriteBatchItem = _batcher.CreateBatchItem();
				spriteBatchItem.Texture = spriteFont.Texture;
				spriteBatchItem.SortKey = sortKey;
				_texCoordTL.X = (float)ptr->BoundsInTexture.X * spriteFont.Texture.TexelWidth;
				_texCoordTL.Y = (float)ptr->BoundsInTexture.Y * spriteFont.Texture.TexelHeight;
				_texCoordBR.X = (float)(ptr->BoundsInTexture.X + ptr->BoundsInTexture.Width) * spriteFont.Texture.TexelWidth;
				_texCoordBR.Y = (float)(ptr->BoundsInTexture.Y + ptr->BoundsInTexture.Height) * spriteFont.Texture.TexelHeight;
				_TuckTextureCoordinates(spriteFont.Texture, ref _texCoordTL, ref _texCoordBR);
				if ((effects & SpriteEffects.FlipVertically) != SpriteEffects.None)
				{
					float y = _texCoordBR.Y;
					_texCoordBR.Y = _texCoordTL.Y;
					_texCoordTL.Y = y;
				}
				if ((effects & SpriteEffects.FlipHorizontally) != SpriteEffects.None)
				{
					float x = _texCoordBR.X;
					_texCoordBR.X = _texCoordTL.X;
					_texCoordTL.X = x;
				}
				if (rotation == 0f)
				{
					spriteBatchItem.Set(position2.X, position2.Y, (float)ptr->BoundsInTexture.Width * scale.X, (float)ptr->BoundsInTexture.Height * scale.Y, color, _texCoordTL, _texCoordBR, layerDepth);
				}
				else
				{
					spriteBatchItem.Set(position2.X, position2.Y, 0f, 0f, (float)ptr->BoundsInTexture.Width * scale.X, (float)ptr->BoundsInTexture.Height * scale.Y, num2, num, color, _texCoordTL, _texCoordBR, layerDepth);
				}
				zero2.X += ptr->Width + ptr->RightSideBearing;
			}
		}
		FlushIfNeeded();
	}

	/// <summary>
	/// Submit a text string of sprites for drawing in the current batch.
	/// </summary>
	/// <param name="spriteFont">A font.</param>
	/// <param name="text">The text which will be drawn.</param>
	/// <param name="position">The drawing location on screen.</param>
	/// <param name="color">A color mask.</param>
	public unsafe void DrawString(SpriteFont spriteFont, StringBuilder text, Vector2 position, Color color)
	{
		CheckValid(spriteFont, text);
		position = new Vector2((int)position.X, (int)position.Y);
		float sortKey = ((_sortMode == SpriteSortMode.Texture) ? spriteFont.Texture.SortingKey : 0);
		Vector2 zero = Vector2.Zero;
		bool flag = true;
		fixed (SpriteFont.Glyph* glyphs = spriteFont.Glyphs)
		{
			for (int i = 0; i < text.Length; i++)
			{
				char c = text[i];
				switch (c)
				{
				case '\n':
					zero.X = 0f;
					zero.Y += spriteFont.LineSpacing;
					flag = true;
					continue;
				case '\r':
					continue;
				}
				int glyphIndexOrDefault = spriteFont.GetGlyphIndexOrDefault(c);
				SpriteFont.Glyph* ptr = glyphs + glyphIndexOrDefault;
				if (flag)
				{
					zero.X = Math.Max(ptr->LeftSideBearing, 0f);
					flag = false;
				}
				else
				{
					zero.X += spriteFont.Spacing + ptr->LeftSideBearing;
				}
				Vector2 vector = zero;
				vector.X += ptr->Cropping.X;
				vector.Y += ptr->Cropping.Y;
				vector += position;
				SpriteBatchItem spriteBatchItem = _batcher.CreateBatchItem();
				spriteBatchItem.Texture = spriteFont.Texture;
				spriteBatchItem.SortKey = sortKey;
				_texCoordTL.X = (float)ptr->BoundsInTexture.X * spriteFont.Texture.TexelWidth;
				_texCoordTL.Y = (float)ptr->BoundsInTexture.Y * spriteFont.Texture.TexelHeight;
				_texCoordBR.X = (float)(ptr->BoundsInTexture.X + ptr->BoundsInTexture.Width) * spriteFont.Texture.TexelWidth;
				_texCoordBR.Y = (float)(ptr->BoundsInTexture.Y + ptr->BoundsInTexture.Height) * spriteFont.Texture.TexelHeight;
				_TuckTextureCoordinates(spriteFont.Texture, ref _texCoordTL, ref _texCoordBR);
				spriteBatchItem.Set(vector.X, vector.Y, ptr->BoundsInTexture.Width, ptr->BoundsInTexture.Height, color, _texCoordTL, _texCoordBR, 0f);
				zero.X += ptr->Width + ptr->RightSideBearing;
			}
		}
		FlushIfNeeded();
	}

	/// <summary>
	/// Submit a text string of sprites for drawing in the current batch.
	/// </summary>
	/// <param name="spriteFont">A font.</param>
	/// <param name="text">The text which will be drawn.</param>
	/// <param name="position">The drawing location on screen.</param>
	/// <param name="color">A color mask.</param>
	/// <param name="rotation">A rotation of this string.</param>
	/// <param name="origin">Center of the rotation. 0,0 by default.</param>
	/// <param name="scale">A scaling of this string.</param>
	/// <param name="effects">Modificators for drawing. Can be combined.</param>
	/// <param name="layerDepth">A depth of the layer of this string.</param>
	public void DrawString(SpriteFont spriteFont, StringBuilder text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
	{
		Vector2 scale2 = new Vector2(scale, scale);
		DrawString(spriteFont, text, position, color, rotation, origin, scale2, effects, layerDepth);
	}

	/// <summary>
	/// Submit a text string of sprites for drawing in the current batch.
	/// </summary>
	/// <param name="spriteFont">A font.</param>
	/// <param name="text">The text which will be drawn.</param>
	/// <param name="position">The drawing location on screen.</param>
	/// <param name="color">A color mask.</param>
	/// <param name="rotation">A rotation of this string.</param>
	/// <param name="origin">Center of the rotation. 0,0 by default.</param>
	/// <param name="scale">A scaling of this string.</param>
	/// <param name="effects">Modificators for drawing. Can be combined.</param>
	/// <param name="layerDepth">A depth of the layer of this string.</param>
	public unsafe void DrawString(SpriteFont spriteFont, StringBuilder text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
	{
		CheckValid(spriteFont, text);
		position = new Vector2((int)position.X, (int)position.Y);
		float sortKey = 0f;
		switch (_sortMode)
		{
		case SpriteSortMode.Texture:
			sortKey = spriteFont.Texture.SortingKey;
			break;
		case SpriteSortMode.FrontToBack:
			sortKey = layerDepth;
			break;
		case SpriteSortMode.BackToFront:
			sortKey = 0f - layerDepth;
			break;
		}
		Vector2 zero = Vector2.Zero;
		bool flag = (effects & SpriteEffects.FlipVertically) == SpriteEffects.FlipVertically;
		bool flag2 = (effects & SpriteEffects.FlipHorizontally) == SpriteEffects.FlipHorizontally;
		if (flag || flag2)
		{
			SpriteFont.CharacterSource text2 = new SpriteFont.CharacterSource(text);
			spriteFont.MeasureString(ref text2, out var size);
			if (flag2)
			{
				origin.X *= -1f;
				zero.X = 0f - size.X;
			}
			if (flag)
			{
				origin.Y *= -1f;
				zero.Y = (float)spriteFont.LineSpacing - size.Y;
			}
		}
		Matrix matrix = Matrix.Identity;
		float num = 0f;
		float num2 = 0f;
		if (rotation == 0f)
		{
			matrix.M11 = (flag2 ? (0f - scale.X) : scale.X);
			matrix.M22 = (flag ? (0f - scale.Y) : scale.Y);
			matrix.M41 = (zero.X - origin.X) * matrix.M11 + position.X;
			matrix.M42 = (zero.Y - origin.Y) * matrix.M22 + position.Y;
		}
		else
		{
			num = (float)Math.Cos(rotation);
			num2 = (float)Math.Sin(rotation);
			matrix.M11 = (flag2 ? (0f - scale.X) : scale.X) * num;
			matrix.M12 = (flag2 ? (0f - scale.X) : scale.X) * num2;
			matrix.M21 = (flag ? (0f - scale.Y) : scale.Y) * (0f - num2);
			matrix.M22 = (flag ? (0f - scale.Y) : scale.Y) * num;
			matrix.M41 = (zero.X - origin.X) * matrix.M11 + (zero.Y - origin.Y) * matrix.M21 + position.X;
			matrix.M42 = (zero.X - origin.X) * matrix.M12 + (zero.Y - origin.Y) * matrix.M22 + position.Y;
		}
		Vector2 zero2 = Vector2.Zero;
		bool flag3 = true;
		fixed (SpriteFont.Glyph* glyphs = spriteFont.Glyphs)
		{
			for (int i = 0; i < text.Length; i++)
			{
				char c = text[i];
				switch (c)
				{
				case '\n':
					zero2.X = 0f;
					zero2.Y += spriteFont.LineSpacing;
					flag3 = true;
					continue;
				case '\r':
					continue;
				}
				int glyphIndexOrDefault = spriteFont.GetGlyphIndexOrDefault(c);
				SpriteFont.Glyph* ptr = glyphs + glyphIndexOrDefault;
				if (flag3)
				{
					zero2.X = Math.Max(ptr->LeftSideBearing, 0f);
					flag3 = false;
				}
				else
				{
					zero2.X += spriteFont.Spacing + ptr->LeftSideBearing;
				}
				Vector2 position2 = zero2;
				if (flag2)
				{
					position2.X += ptr->BoundsInTexture.Width;
				}
				position2.X += ptr->Cropping.X;
				if (flag)
				{
					position2.Y += ptr->BoundsInTexture.Height - spriteFont.LineSpacing;
				}
				position2.Y += ptr->Cropping.Y;
				Vector2.Transform(ref position2, ref matrix, out position2);
				SpriteBatchItem spriteBatchItem = _batcher.CreateBatchItem();
				spriteBatchItem.Texture = spriteFont.Texture;
				spriteBatchItem.SortKey = sortKey;
				_texCoordTL.X = (float)ptr->BoundsInTexture.X * spriteFont.Texture.TexelWidth;
				_texCoordTL.Y = (float)ptr->BoundsInTexture.Y * spriteFont.Texture.TexelHeight;
				_texCoordBR.X = (float)(ptr->BoundsInTexture.X + ptr->BoundsInTexture.Width) * spriteFont.Texture.TexelWidth;
				_texCoordBR.Y = (float)(ptr->BoundsInTexture.Y + ptr->BoundsInTexture.Height) * spriteFont.Texture.TexelHeight;
				_TuckTextureCoordinates(spriteFont.Texture, ref _texCoordTL, ref _texCoordBR);
				if ((effects & SpriteEffects.FlipVertically) != SpriteEffects.None)
				{
					float y = _texCoordBR.Y;
					_texCoordBR.Y = _texCoordTL.Y;
					_texCoordTL.Y = y;
				}
				if ((effects & SpriteEffects.FlipHorizontally) != SpriteEffects.None)
				{
					float x = _texCoordBR.X;
					_texCoordBR.X = _texCoordTL.X;
					_texCoordTL.X = x;
				}
				if (rotation == 0f)
				{
					spriteBatchItem.Set(position2.X, position2.Y, (float)ptr->BoundsInTexture.Width * scale.X, (float)ptr->BoundsInTexture.Height * scale.Y, color, _texCoordTL, _texCoordBR, layerDepth);
				}
				else
				{
					spriteBatchItem.Set(position2.X, position2.Y, 0f, 0f, (float)ptr->BoundsInTexture.Width * scale.X, (float)ptr->BoundsInTexture.Height * scale.Y, num2, num, color, _texCoordTL, _texCoordBR, layerDepth);
				}
				zero2.X += ptr->Width + ptr->RightSideBearing;
			}
		}
		FlushIfNeeded();
	}

	/// <summary>
	/// Immediately releases the unmanaged resources used by this object.
	/// </summary>
	/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
	protected override void Dispose(bool disposing)
	{
		if (!base.IsDisposed && disposing && _spriteEffect != null)
		{
			_spriteEffect.Dispose();
			_spriteEffect = null;
		}
		base.Dispose(disposing);
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.0.0.9375' (yours is '9.1.0.7988')
