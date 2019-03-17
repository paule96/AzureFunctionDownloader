# AzureFunctionDownloader
An Azure Function which takes urls of files and generate a downloadable zip archive out of them.

# Getting started

After you have deployed the function you can use it like the following HTTP GET request:

```
https://yourfunctionurl/api/Downloader?filenameInZip.exe=https://urltofile/file.exe
```

The function also supports POST. When using POST your HTTP Body needs to be set to application/json and should contain the values like this:

```json
[
	{
		"Key": 'filenameInZip.exe',
		"Value": 'https://urltofile/file.exe'
	}
]
```

The URL for Post are:
```
https://yourfunctionurl/api/Downloader
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details
