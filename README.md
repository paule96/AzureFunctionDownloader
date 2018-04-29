# AzureFunctionDownloader
A Azure Function you can send http url's of files that put it in a zip Archiv.

# Use the Function

If you deploy the function you can use this like this with get requests:

```
https://yourfunctionurl/api/Downloader?filenameInZip.exe=https://urltofile/file.exe
```

This also supports post. Your Body must set to application/json and looks like this:

```
[
	{
		Key: 'filenameInZip.exe',
		Value: 'https://urltofile/file.exe'
	}
]
```

The URL for Post are:
```
https://yourfunctionurl/api/Downloader
```
