import unittest
from unittest.mock import Mock, patch
import grpc
import requests
import produto_pb2
from server import extrair_produtos, ScraperService

HTML = '<article class="product_pod"><h3><a title="Desenho autoral">Livro</a></h3><p class="price_color">£12.50</p></article>'


class Abortado(Exception):
    pass


class TestScraper(unittest.TestCase):
    def test_extrai_texto_e_limita_cinco(self):
        produtos = extrair_produtos(HTML * 8)
        self.assertEqual(5, len(produtos))
        self.assertEqual("Desenho autoral", produtos[0].titulo)
        self.assertEqual("£12.50", produtos[0].preco)

    def test_estrutura_invalida_nao_e_catalogo_vazio(self):
        for html in ("<html>erro</html>", '<article class="product_pod"></article>'):
            with self.assertRaises(ValueError):
                extrair_produtos(html)

    @patch("server.requests.get", side_effect=requests.Timeout)
    def test_timeout_vira_unavailable(self, get):
        context = Mock()
        context.is_active.return_value = True
        context.abort.side_effect = Abortado
        with self.assertRaises(Abortado):
            ScraperService().ObterProdutos(produto_pb2.VazioRequest(), context)
        context.abort.assert_called_once_with(grpc.StatusCode.UNAVAILABLE, "Fonte de referencias indisponivel")
        self.assertEqual((3, 5), get.call_args.kwargs["timeout"])

    @patch("server.requests.get")
    def test_html_invalido_vira_data_loss(self, get):
        get.return_value.text = "<html>sem produtos</html>"
        context = Mock()
        context.is_active.return_value = True
        context.abort.side_effect = Abortado
        with self.assertRaises(Abortado):
            ScraperService().ObterProdutos(produto_pb2.VazioRequest(), context)
        context.abort.assert_called_once_with(grpc.StatusCode.DATA_LOSS, "Pagina de referencias com estrutura inesperada")


if __name__ == "__main__":
    unittest.main()
