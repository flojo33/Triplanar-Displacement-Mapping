/*
 * Include file for functions used by all of the Compact Material shaders.
 */
 float grayscale(float4 input) {
    return max(input.r,max(input.g,input.b));
}