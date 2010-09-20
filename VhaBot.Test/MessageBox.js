var msg = "";
if (WScript.Arguments.length > 0) {
  for (i = 0; i < WScript.Arguments.length; i++) {
      msg = msg + WScript.Arguments.Item(i) + ' ';
  }
}
WScript.Echo(msg);