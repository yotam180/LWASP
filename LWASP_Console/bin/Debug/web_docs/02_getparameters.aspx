<@
if (!_GET.ContainsKey("name")) {
	echo("You must specify your name in the query string. Example: <pre>02_getparameters.aspx?name=Yotam</pre>");
}
else {
	echo("Hello, " + _GET["name"]);
}
@>