document.addEventListener('DOMContentLoaded', function () {
    const productForm = document.getElementById('productForm');
    const editProductForm = document.getElementById('editProductForm');
    const productTable = document.getElementById('productTable');

    function saveProducts(products) {
        localStorage.setItem('products', JSON.stringify(products));
    }

    function getProducts() {
        return JSON.parse(localStorage.getItem('products') || '[]');
    }

    if (productForm) {
        productForm.addEventListener('submit', function (e) {
            e.preventDefault();

            const name = document.getElementById('name').value;
            const stock = document.getElementById('stock').value;
            const unit = document.getElementById('unit').value;
            const price = document.getElementById('price').value;

            const products = getProducts();
            products.push({ id: Date.now(), name, stock, unit, price });
            saveProducts(products);

            this.reset();

            alert('Ürün başarıyla eklendi!');
        });
    }

    if (editProductForm) {
        const productId = new URLSearchParams(window.location.search).get('id');
        if (productId) {
            const products = getProducts();
            const product = products.find(p => p.id == productId);
            if (product) {
                document.getElementById('productId').value = product.id;
                document.getElementById('name').value = product.name;
                document.getElementById('stock').value = product.stock;
                document.getElementById('unit').value = product.unit;
                document.getElementById('price').value = product.price;
            }
        }

        editProductForm.addEventListener('submit', function (e) {
            e.preventDefault();

            const id = document.getElementById('productId').value;
            const name = document.getElementById('name').value;
            const stock = document.getElementById('stock').value;
            const unit = document.getElementById('unit').value;
            const price = document.getElementById('price').value;

            const products = getProducts();
            const index = products.findIndex(p => p.id == id);
            if (index !== -1) {
                products[index] = { id, name, stock, unit, price };
                saveProducts(products);
                alert('Ürün başarıyla güncellendi!');
                window.location.href = 'index.html';
            }
        });
    }

    if (productTable) {
        const products = getProducts();
        const tbody = productTable.getElementsByTagName('tbody')[0];

        products.forEach(product => {
            const newRow = tbody.insertRow();
            newRow.innerHTML = `
                <td>${product.name}</td>
                <td>${product.stock}</td>
                <td>${product.unit}</td>
                <td>${parseFloat(product.price).toFixed(2)}</td>
                <td>
                    <a href="edit-product.html?id=${product.id}" class="edit-link">Düzenle</a>
                </td>
            `;
        });
    }
});