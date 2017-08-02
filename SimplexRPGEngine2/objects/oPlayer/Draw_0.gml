/// @desc Render and handle input

libPlayerHandleInput();
libPlayerDraw();

draw_text_colored(x, y - 64, "This is" + _sc("colored text") + _sc("even", c_lime) + _sc("more", c_blue) + _sc("colors", c_red) + "##Icon in" + _sp(sItems, 4) + "text", 256, fntPixel);
draw_text_colored(x, y - 120, "This is" + _sc("colored text") + _sc("even", c_lime) + _sc("more", c_blue) + _sc("colors", c_red) + "##Icon in" + _sp(sItems, 4) + "text", 256, fntPixelTiny);
draw_line_width(x + 256, y - 128, x + 256, y + 128, 4); 

draw_text(oCamera.v_nullPosX + 780, oCamera.v_nullPosY, fps);