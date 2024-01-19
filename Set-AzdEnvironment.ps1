write-host "Setting AZD current .env as local environment variables"
azd env get-values | ForEach-Object { set-Item -Path ('Env:' + $_.Split('=')[0]) -Value ($_.Split('=')[1].Replace("""","")) }
