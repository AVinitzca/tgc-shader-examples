// ---------------------------------------------------------
// Ejemplo shader Minimo:
// ---------------------------------------------------------

/**************************************************************************************/
/* Variables comunes */
/**************************************************************************************/

//Matrices de transformacion
float4x4 matWorld; //Matriz de transformacion World
float4x4 matViewProj;
float4x4 matWorldView; //Matriz World * View
float4x4 matWorldViewProj; //Matriz World * View * Projection
float4x4 matInverseTransposeWorld; //Matriz Transpose(Invert(World))

float4 center;
float3 effectVector;
float time = 0;
float factor;

//Textura para DiffuseMap
texture texDiffuseMap;
sampler2D diffuseMap = sampler_state
{
    Texture = (texDiffuseMap);
    ADDRESSU = WRAP;
    ADDRESSV = WRAP;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};

//Input del Vertex Shader
struct VS_INPUT
{
    float4 Position : POSITION0;
	float4 Normal : NORMAL0;
    float4 Color : COLOR0;
    float2 Texcoord : TEXCOORD0;
};

//Output del Vertex Shader
struct VS_OUTPUT
{
    float4 Position : POSITION0;
	float4 Normal : NORMAL0;
    float2 Texcoord : TEXCOORD0;
    float4 Color : COLOR0;
};

float3 dotLength(float3 vectorOne, float3 vectorTwo)
{
	return dot(vectorOne, vectorTwo) / length(vectorOne) / length(vectorTwo);
}

VS_OUTPUT vs_main(VS_INPUT Input)
{
    VS_OUTPUT Output;

	//Proyectar posicion
    Output.Position = mul(Input.Position, matWorldViewProj);

	Output.Normal = Input.Normal;

	//Propago las coordenadas de textura
    Output.Texcoord = Input.Texcoord;

	//Propago el color x vertice
    Output.Color = Input.Color;

    return Output;
}

VS_OUTPUT vs_expansion(VS_INPUT Input)
{
	Input.Position = lerp(Input.Position, center, sin(time) - 1);
	return vs_main(Input);
}

VS_OUTPUT vs_extrude(VS_INPUT Input)
{
	VS_OUTPUT Output;

	float pos = clamp(abs(dotLength(Input.Normal, effectVector)), factor, 10);
	
	Input.Position.x *= 1 + pos;
	Input.Position.y *= 1 + pos;
	Input.Position.z *= 1 + pos;

	Output.Position = mul(Input.Position, matWorldViewProj);;
	
	Output.Normal = Input.Normal;

	Output.Texcoord = Input.Texcoord;

	Output.Color = Input.Color;

	return Output;
}

VS_OUTPUT vs_identity_plane_extrude(VS_INPUT Input)
{
	VS_OUTPUT Output;

	float timeFactor = time * 10 * factor;
	float3 planeNormal = float3(cos(timeFactor), 1, sin(timeFactor));

	float parallel = dot(Input.Position, planeNormal);
	float extrude = (parallel > -0.1 && parallel < 0.1) * 0.1;

	Input.Position.x *= 1 + extrude;
	Input.Position.y *= 1 + extrude;
	Input.Position.z *= 1 + extrude;

	Output.Position = mul(Input.Position, matWorldViewProj);;

	Output.Normal = Input.Normal;

	Output.Texcoord = Input.Texcoord;

	Output.Color = Input.Color;

	return Output;
}

VS_OUTPUT vs_planar_extrude(VS_INPUT Input)
{
	VS_OUTPUT Output;
	
	float parallel = dotLength(Input.Position, effectVector);

	float extrude = (parallel > -0.1 && parallel < 0.1) * 0.2;

	Input.Position.x *= 1 + extrude;
	Input.Position.y *= 1 + extrude;
	Input.Position.z *= 1 + extrude;

	Output.Position = mul(Input.Position, matWorldViewProj);;

	Output.Normal = Input.Normal;

	Output.Texcoord = Input.Texcoord;

	Output.Color = Input.Color;

	return Output;
}

float4 ps_main(VS_OUTPUT Input) : COLOR0
{
	return tex2D(diffuseMap, Input.Texcoord);
}

float4 ps_texture_cycling(VS_OUTPUT Input) : COLOR0
{
	Input.Texcoord.x += sin(time * 2) * 0.1;
	Input.Texcoord.y += cos(time * 2) * 0.1;
	return ps_main(Input);
}

float4 ps_color_cycling(VS_OUTPUT Input) : COLOR0
{
	float3 rotated = float3(cos(time), 1, sin(time));
	float3 normal = float3(sin(time), 1, cos(time));
	float3 other = float3(tan(time), 1, -tan(time));

	float r = abs(dotLength(rotated, Input.Normal));
	float g = abs(dotLength(normal, Input.Normal));
	float b = abs(dotLength(other, Input.Normal));

	return float4(r, g, b, 1);
}

struct VS_LIGHT_OUTPUT
{
	float4 Position : POSITION0;
	float2 Texcoord : TEXCOORD0;
	float4 InterpolatedPosition : TEXCOORD1;
};

VS_LIGHT_OUTPUT vs_lightstruck(VS_INPUT Input)
{
	VS_LIGHT_OUTPUT Output;

	Output.Position = mul(Input.Position, matWorldViewProj);
	
	Output.InterpolatedPosition = Input.Position;

	Output.Texcoord = Input.Texcoord;

	return Output;
}

float4 ps_inner_light(VS_LIGHT_OUTPUT Input) : COLOR0
{
	float4 textureColor = tex2D(diffuseMap, Input.Texcoord);
	float innerLight = clamp(dotLength(Input.InterpolatedPosition, effectVector), 0.1, 1);

	return textureColor * innerLight;
}

struct VS_EXTRUDED_OUTPUT
{
	float4 Position : POSITION0;
	float2 Texcoord : TEXCOORD0;
	float InRange : TEXCOORD1;
};




VS_EXTRUDED_OUTPUT vs_identity_plane_extrude_with_position(VS_LIGHT_OUTPUT Input)
{
	VS_EXTRUDED_OUTPUT Output;

	float timeFactor = time * 10 * factor;
	float3 planeNormal = float3(cos(timeFactor), 1, sin(timeFactor));

	float parallel = dot(Input.Position, planeNormal);
	float extrude = (parallel > -0.1 && parallel < 0.1) * 0.1;

	Output.InRange = extrude;

	float4 pos = Input.Position * (1 + extrude);
	pos.w = 1;

	Output.Position = mul(pos, matWorldViewProj);;
	
	Output.Texcoord = Input.Texcoord;

	return Output;
}

float4 ps_extrude(VS_EXTRUDED_OUTPUT Input) : COLOR0
{
	return tex2D(diffuseMap, Input.Texcoord) + float4(0, 0, Input.InRange * 10 * (sin(time * 2) + 1.5), 1);
}


technique RenderMesh
{
    pass Pass_0
    {
        VertexShader = compile vs_3_0 vs_main();
        PixelShader = compile ps_3_0 ps_main();
    }
}

technique Expansion
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_expansion();
		PixelShader = compile ps_3_0 ps_main();
	}
};

technique Extrude
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_extrude();
		PixelShader = compile ps_3_0 ps_main();		
	}
};

technique PlanarExtrude
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_planar_extrude();
		PixelShader = compile ps_3_0 ps_main();
	}
};

technique IdentityPlaneExtrude
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_identity_plane_extrude();
		PixelShader = compile ps_3_0 ps_main();
	}
};

technique TextureCycling
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_main();
		PixelShader = compile ps_3_0 ps_texture_cycling();
	}
};

technique ColorCycling
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_main();
		PixelShader = compile ps_3_0 ps_color_cycling();
	}
};

technique InnerLight
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_lightstruck();
		PixelShader = compile ps_3_0 ps_inner_light();
	}
};

technique ExtrudeCombined
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_identity_plane_extrude_with_position();
		PixelShader = compile ps_3_0 ps_extrude();
	}
};