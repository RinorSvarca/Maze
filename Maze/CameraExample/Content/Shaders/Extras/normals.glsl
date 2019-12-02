﻿uniform sampler2D color;
uniform sampler2D normal;
uniform sampler2D depth;

in vec2 uv;

void main() {

vec3 color = texture(depth, uv).rgb;
		
	gl_FragColor = vec4(color, 1.0);
}