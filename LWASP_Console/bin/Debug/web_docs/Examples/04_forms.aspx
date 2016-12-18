<@
string formShown = "none";
if (_CONNECTION.http_method == "GET")
{
	formShown = "";
}
else
{
	if (!_POST.ContainsKey("username"))
	{
		echo("Problem - username parameter was not specified.");
		return;
	}
	echo("Hello, " + _POST["username"] + "!!");
}
@>

<form style="display: <@echo(formShown);@>" action="04_forms.aspx" method="POST">
<input type="text" name="username" />
<input type="submit" />
</form>