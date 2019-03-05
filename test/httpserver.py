#!/usr/bin/env python3

import sys
import os,stat,os.path 

port=8888

import http.server 
HTTPServer = http.server.HTTPServer 
Handler = http.server.SimpleHTTPRequestHandler
    
server = HTTPServer( ('127.0.0.1',port), Handler)
print ("Listening on port "+str(port))
while 1:
    server.handle_request()



