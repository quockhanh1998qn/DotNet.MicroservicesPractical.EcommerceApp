$ErrorActionPreference = 'Continue'

function Test-Endpoint([string]$Name, [string]$Url, [string]$Method = 'GET', [string]$Body = $null) {
	try {
		$params = @{ Uri = $Url; Method = $Method; UseBasicParsing = $true; TimeoutSec = 15 }
		if ($Body) {
			$params.Body = $Body
			$params.ContentType = 'application/json'
		}
		$r = Invoke-WebRequest @params
		$content = if ($r.Content.Length -gt 100) { $r.Content.Substring(0, 100) + '...' } else { $r.Content }
		"{0,-45} {1}  {2}" -f $Name, $r.StatusCode, $content
	} catch {
		$sc = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 'NET' }
		"{0,-45} {1}  {2}" -f $Name, $sc, $_.Exception.Message
	}
}

Write-Host "=== DIRECT to APIs ==="
Test-Endpoint "product.api      GET  /api/products"           "http://localhost:5102/api/products"
Test-Endpoint "customer.api     GET  /api/customers"          "http://localhost:5103/api/customers"
Test-Endpoint "basket.api       GET  /api/baskets/swn"        "http://localhost:5004/api/baskets/swn"
Test-Endpoint "ordering.api     GET  /api/orders/swn"         "http://localhost:5005/api/orders/swn"
Test-Endpoint "inventory.api    GET  stock(empty)"            "http://localhost:5006/api/inventories/stock/P-001"
Test-Endpoint "inventory.api    POST purchase"                "http://localhost:5006/api/inventories/purchase" "POST" '{"itemNo":"P-001","quantity":50,"externalDocumentNo":"PO-1"}'
Test-Endpoint "inventory.api    GET  stock(after purchase)"   "http://localhost:5006/api/inventories/stock/P-001"

Write-Host ""
Write-Host "=== Via Ocelot Gateway (port 5000) ==="
Test-Endpoint "gw  GET  /Products"                  "http://localhost:5000/Products"
Test-Endpoint "gw  GET  /Customers"                 "http://localhost:5000/Customers"
Test-Endpoint "gw  GET  /Inventories/stock/P-001"   "http://localhost:5000/Inventories/stock/P-001"
Test-Endpoint "gw  GET  /Baskets/swn (auth)"        "http://localhost:5000/Baskets/swn"
Test-Endpoint "gw  GET  /Orders/swn (auth)"         "http://localhost:5000/Orders/swn"

Write-Host ""
Write-Host "=== Basket -> Inventory gRPC stock check (T6.7) ==="
Test-Endpoint "basket.api PUT cart (qty=10, stock=50)" "http://localhost:5004/api/baskets" "POST" '{"username":"swn","items":[{"productNo":"P-001","productName":"Test","quantity":10,"price":5.0}]}'
Test-Endpoint "basket.api PUT cart (qty=999, stock=50)" "http://localhost:5004/api/baskets" "POST" '{"username":"swn","items":[{"productNo":"P-001","productName":"Test","quantity":999,"price":5.0}]}'
