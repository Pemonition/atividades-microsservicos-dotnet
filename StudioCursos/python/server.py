"""Evolucao do servidor enviado pelo professor: timeout, erros e bind local."""
from concurrent import futures
import logging
import os

import grpc
import requests
from bs4 import BeautifulSoup
import produto_pb2
import produto_pb2_grpc


def extrair_produtos(html):
    artigos = BeautifulSoup(html, "html.parser").select("article.product_pod")[:5]
    if not artigos:
        raise ValueError("Pagina sem produtos reconhecidos")
    produtos = []
    for artigo in artigos:
        link = artigo.select_one("h3 a[title]")
        preco = artigo.select_one("p.price_color")
        if link is None or preco is None or not link["title"].strip() or not preco.get_text(strip=True):
            raise ValueError("Estrutura de produto incompleta")
        produtos.append(produto_pb2.ProdutoDto(titulo=link["title"].strip(), preco=preco.get_text(strip=True)))
    return produtos


class ScraperService(produto_pb2_grpc.ScraperServiceServicer):
    def ObterProdutos(self, request, context):
        if not context.is_active():
            context.abort(grpc.StatusCode.CANCELLED, "Chamada cancelada")
        try:
            response = requests.get(os.environ.get("SCRAPER_URL", "https://books.toscrape.com/"),
                                    timeout=(3, 5), headers={"User-Agent": "StudioCursosAula/1.0"})
            response.raise_for_status()
            response.encoding = "utf-8"
            produtos = extrair_produtos(response.text)
        except requests.RequestException:
            context.abort(grpc.StatusCode.UNAVAILABLE, "Fonte de referencias indisponivel")
        except ValueError:
            context.abort(grpc.StatusCode.DATA_LOSS, "Pagina de referencias com estrutura inesperada")
        if not context.is_active():
            context.abort(grpc.StatusCode.CANCELLED, "Chamada cancelada")
        return produto_pb2.ListaProdutosResponse(produtos=produtos)


def serve():
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    produto_pb2_grpc.add_ScraperServiceServicer_to_server(ScraperService(), server)
    address = os.environ.get("GRPC_BIND", "127.0.0.1:50051")
    if not server.add_insecure_port(address):
        raise RuntimeError("Nao foi possivel abrir a porta gRPC")
    server.start()
    print(f"StudioCursos Python gRPC em {address}", flush=True)
    try:
        server.wait_for_termination()
    except KeyboardInterrupt:
        server.stop(3).wait()


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    serve()
