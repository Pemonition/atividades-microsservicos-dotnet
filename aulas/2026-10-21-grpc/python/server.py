from concurrent import futures
import grpc
import requests
from bs4 import BeautifulSoup

import produto_pb2
import produto_pb2_grpc


# Exemplo enviado pelo professor no chat da aula de 21/10/2026.
# O comportamento didatico foi preservado; veja os limites em analise-e-execucao.md.
class ScraperServiceServicer(produto_pb2_grpc.ScraperServiceServicer):
    def ObterProdutos(self, request, context):
        print("Recebida requisição do cliente! Fazendo o Web Scraping...")
        url = "http://books.toscrape.com/"
        response = requests.get(url)
        response.encoding = "utf-8"
        produtos_encontrados = []
        if response.status_code == 200:
            soup = BeautifulSoup(response.text, "html.parser")
            livros = soup.find_all("article", class_="product_pod", limit=5)
            for livro in livros:
                titulo = livro.find("h3").find("a")["title"]
                preco = livro.find("p", class_="price_color").text
                produtos_encontrados.append(
                    produto_pb2.ProdutoDto(titulo=titulo, preco=preco)
                )
        print(f"Scraping concluído! Retornando {len(produtos_encontrados)} produtos.")
        return produto_pb2.ListaProdutosResponse(produtos=produtos_encontrados)


def serve():
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    produto_pb2_grpc.add_ScraperServiceServicer_to_server(ScraperServiceServicer(), server)
    server.add_insecure_port("[::]:50051")
    server.start()
    print("Servidor Python gRPC rodando na porta 50051...")
    server.wait_for_termination()


if __name__ == "__main__":
    serve()
