﻿using Microsoft.AspNetCore.Mvc.Formatters;
using Morcatko.AspNetCore.JsonMergePatch.Builder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Linq;

namespace Morcatko.AspNetCore.JsonMergePatch.Formatters
{
	class NewtonsoftJsonMergePatchSerializer : JsonSerializer
	{
		private readonly IList _listContainer;
		private readonly Type _jsonMergePatchType;
		private readonly Type _innerModelType;
		private readonly JsonSerializerSettings _serializerSettings;
		private readonly JsonMergePatchOptions _jsonMergePatchOptions;
		private readonly JsonSerializer _innerjsonSerializer;

		public NewtonsoftJsonMergePatchSerializer(
			IList listContainer,
			Type jsonMergePatchType,
			Type innerModelType,
			JsonSerializer innerJsonSerializer,
			JsonSerializerSettings serializerSettings,
			JsonMergePatchOptions jsonMergePatchOptions)
		{
			_listContainer = listContainer;
			_jsonMergePatchType = jsonMergePatchType;
			_innerModelType = innerModelType;
			_serializerSettings = serializerSettings;
			_jsonMergePatchOptions = jsonMergePatchOptions;
			_innerjsonSerializer = innerJsonSerializer;
		}

		private JsonMergePatchDocument CreatePatchDocument(JObject jObject, JsonSerializer jsonSerializer)
		{
			var jsonMergePatchDocument = PatchBuilder.CreatePatchDocument(_jsonMergePatchType, _innerModelType, jObject, jsonSerializer, _jsonMergePatchOptions);
			jsonMergePatchDocument.ContractResolver = _serializerSettings.ContractResolver;
			return jsonMergePatchDocument;
		}

		public new object Deserialize(JsonReader reader, Type objectType)
		{
			var jToken = JToken.Load(reader);

			switch (jToken)
			{
				case JObject jObject:
					if (_listContainer != null)
						throw new ArgumentException("Received object when array was expected"); //This could be handled by returnin list with single item

					var jsonMergePatchDocument = CreatePatchDocument(jObject, _innerjsonSerializer);
					return InputFormatterResult.Success(jsonMergePatchDocument);
				case JArray jArray:
					if (_listContainer == null)
						throw new ArgumentException("Received array when object was expected");

					foreach (var jObject in jArray.OfType<JObject>())
					{
						_listContainer.Add(CreatePatchDocument(jObject, _innerjsonSerializer));
					}
					return InputFormatterResult.Success(_listContainer);
			}

			return InputFormatterResult.Failure();
		}
	}
}
