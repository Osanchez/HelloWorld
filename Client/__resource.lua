resource_manifest_version '44febabe-d386-4d18-afbe-5e627f4af937'

client_script {
	'Client.net.dll'
}

server_scripts {
	'Server.net.dll'
}

ui_page('ui/html/index.html')

files({
    'ui/html/index.html',
    'ui/html/script.js',
    'ui/html/style.css',
    'ui/html/cursor.png'
})
