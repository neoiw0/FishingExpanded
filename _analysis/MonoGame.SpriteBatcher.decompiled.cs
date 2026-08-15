using System;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// This class handles the queueing of batch items into the GPU by creating the triangle tesselations
/// that are used to draw the sprite textures. This class supports int.MaxValue number of sprites to be
/// batched and will process them into short.MaxValue groups (strided by 6 for the number of vertices
/// sent to the GPU). 
/// </summary>
internal class SpriteBatcher
{
	/// <summary>
	/// Initialization size for the batch item list and queue.
	/// </summary>
	private const int InitialBatchSize = 256;

	/// <summary>
	/// The maximum number of batch items that can be processed per iteration
	/// </summary>
	private const int MaxBatchSize = 5461;

	/// <summary>
	/// Initialization size for the vertex array, in batch units.
	/// </summary>
	private const int InitialVertexArraySize = 256;

	/// <summary>
	/// The list of batch items to process.
	/// </summary>
	private SpriteBatchItem[] _batchItemList;

	/// <summary>
	/// Index pointer to the next available SpriteBatchItem in _batchItemList.
	/// </summary>
	private int _batchItemCount;

	/// <summary>
	/// The target graphics device.
	/// </summary>
	private readonly GraphicsDevice _device;

	/// <summary>
	/// Vertex index array. The values in this array never change.
	/// </summary>
	private short[] _index;

	private VertexPositionColorTexture[] _vertexArray;

	public SpriteBatcher(GraphicsDevice device, int capacity = 0)
	{
		_device = device;
		capacity = ((capacity > 0) ? ((capacity + 63) & -64) : 256);
		_batchItemList = new SpriteBatchItem[capacity];
		_batchItemCount = 0;
		for (int i = 0; i < capacity; i++)
		{
			_batchItemList[i] = new SpriteBatchItem();
		}
		EnsureArrayCapacity(capacity);
	}

	/// <summary>
	/// Reuse a previously allocated SpriteBatchItem from the item pool. 
	/// if there is none available grow the pool and initialize new items.
	/// </summary>
	/// <returns></returns>
	public SpriteBatchItem CreateBatchItem()
	{
		if (_batchItemCount >= _batchItemList.Length)
		{
			int num = _batchItemList.Length;
			int num2 = num + num / 2;
			num2 = (num2 + 63) & -64;
			Array.Resize(ref _batchItemList, num2);
			for (int i = num; i < num2; i++)
			{
				_batchItemList[i] = new SpriteBatchItem();
			}
			EnsureArrayCapacity(Math.Min(num2, 5461));
		}
		return _batchItemList[_batchItemCount++];
	}

	/// <summary>
	/// Resize and recreate the missing indices for the index and vertex position color buffers.
	/// </summary>
	/// <param name="numBatchItems"></param>
	private unsafe void EnsureArrayCapacity(int numBatchItems)
	{
		int num = 6 * numBatchItems;
		if (_index != null && num <= _index.Length)
		{
			return;
		}
		short[] array = new short[6 * numBatchItems];
		int num2 = 0;
		if (_index != null)
		{
			_index.CopyTo(array, 0);
			num2 = _index.Length / 6;
		}
		fixed (short* ptr = array)
		{
			short* ptr2 = ptr + num2 * 6;
			int num3 = num2;
			while (num3 < numBatchItems)
			{
				*ptr2 = (short)(num3 * 4);
				ptr2[1] = (short)(num3 * 4 + 1);
				ptr2[2] = (short)(num3 * 4 + 2);
				ptr2[3] = (short)(num3 * 4 + 1);
				ptr2[4] = (short)(num3 * 4 + 3);
				ptr2[5] = (short)(num3 * 4 + 2);
				num3++;
				ptr2 += 6;
			}
		}
		_index = array;
		_vertexArray = new VertexPositionColorTexture[4 * numBatchItems];
	}

	/// <summary>
	/// Sorts the batch items and then groups batch drawing into maximal allowed batch sets that do not
	/// overflow the 16 bit array indices for vertices.
	/// </summary>
	/// <param name="sortMode">The type of depth sorting desired for the rendering.</param>
	/// <param name="effect">The custom effect to apply to the drawn geometry</param>
	public unsafe void DrawBatch(SpriteSortMode sortMode, Effect effect)
	{
		if (effect != null && effect.IsDisposed)
		{
			throw new ObjectDisposedException("effect");
		}
		if (_batchItemCount == 0)
		{
			return;
		}
		if ((uint)(sortMode - 2) <= 2u)
		{
			Array.Sort(_batchItemList, 0, _batchItemCount);
		}
		int num = 0;
		int num2 = _batchItemCount;
		_device._graphicsMetrics._spriteCount += num2;
		while (num2 > 0)
		{
			int start = 0;
			int num3 = 0;
			Texture2D texture2D = null;
			int num4 = num2;
			if (num4 > 5461)
			{
				num4 = 5461;
			}
			fixed (VertexPositionColorTexture* vertexArray = _vertexArray)
			{
				VertexPositionColorTexture* ptr = vertexArray;
				int num5 = 0;
				while (num5 < num4)
				{
					SpriteBatchItem spriteBatchItem = _batchItemList[num];
					if (spriteBatchItem.Texture != texture2D)
					{
						FlushVertexArray(start, num3, effect, texture2D);
						texture2D = spriteBatchItem.Texture;
						start = (num3 = 0);
						ptr = vertexArray;
						_device.Textures[0] = texture2D;
					}
					*ptr = spriteBatchItem.vertexTL;
					ptr[1] = spriteBatchItem.vertexTR;
					ptr[2] = spriteBatchItem.vertexBL;
					ptr[3] = spriteBatchItem.vertexBR;
					spriteBatchItem.Texture = null;
					num5++;
					num++;
					num3 += 4;
					ptr += 4;
				}
			}
			FlushVertexArray(start, num3, effect, texture2D);
			num2 -= num4;
		}
		_batchItemCount = 0;
	}

	/// <summary>
	/// Sends the triangle list to the graphics device. Here is where the actual drawing starts.
	/// </summary>
	/// <param name="start">Start index of vertices to draw. Not used except to compute the count of vertices to draw.</param>
	/// <param name="end">End index of vertices to draw. Not used except to compute the count of vertices to draw.</param>
	/// <param name="effect">The custom effect to apply to the geometry</param>
	/// <param name="texture">The texture to draw.</param>
	private void FlushVertexArray(int start, int end, Effect effect, Texture texture)
	{
		if (start == end)
		{
			return;
		}
		int num = end - start;
		if (effect != null)
		{
			foreach (EffectPass pass in effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				_device.Textures[0] = texture;
				_device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertexArray, 0, num, _index, 0, num / 4 * 2, VertexPositionColorTexture.VertexDeclaration);
			}
			return;
		}
		_device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertexArray, 0, num, _index, 0, num / 4 * 2, VertexPositionColorTexture.VertexDeclaration);
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.0.0.9375' (yours is '9.1.0.7988')
