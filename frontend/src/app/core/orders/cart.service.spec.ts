import { TestBed } from '@angular/core/testing';

import { Dish } from '../menu/menu.models';
import { CartService } from './cart.service';

const FEIJOADA: Dish = { id: 1, date: '2026-10-28', name: 'Feijoada', description: null };
const MOQUECA: Dish = { id: 2, date: '2026-10-28', name: 'Moqueca', description: null };
const OTHER_DAY: Dish = { id: 3, date: '2026-10-29', name: 'Strogonoff', description: null };

describe('CartService', () => {
  let cart: CartService;

  beforeEach(() => {
    cart = TestBed.inject(CartService);
  });

  it('add_novoPrato_entraComQuantidadeUm', () => {
    expect(cart.add(FEIJOADA)).toBe('added');

    expect(cart.lines()).toEqual([{ dishId: 1, dishName: 'Feijoada', quantity: 1 }]);
    expect(cart.date()).toBe('2026-10-28');
  });

  it('add_pratoJaNoCarrinho_somaUmNaQuantidade', () => {
    cart.add(FEIJOADA);
    cart.add(FEIJOADA);

    expect(cart.lines()[0].quantity).toBe(2);
  });

  it('add_quantidadeNoMaximo_recusaEMantem', () => {
    cart.add(FEIJOADA);
    cart.setQuantity(1, 10);

    expect(cart.add(FEIJOADA)).toBe('quantity-limit');
    expect(cart.lines()[0].quantity).toBe(10);
  });

  it('add_pratoDeOutroDia_recusaSemMisturar', () => {
    cart.add(FEIJOADA);

    expect(cart.add(OTHER_DAY)).toBe('wrong-day');
    expect(cart.lines().length).toBe(1);
  });

  it('add_vinteLinhas_recusaAVigesimaPrimeira', () => {
    for (let id = 1; id <= 20; id += 1) {
      cart.add({ ...FEIJOADA, id });
    }

    expect(cart.add({ ...FEIJOADA, id: 21 })).toBe('line-limit');
    expect(cart.lines().length).toBe(20);
  });

  it('setQuantity_foraDoIntervalo_ajustaParaOLimite', () => {
    cart.add(FEIJOADA);

    cart.setQuantity(1, 99);
    expect(cart.lines()[0].quantity).toBe(10);
    cart.setQuantity(1, 0);
    expect(cart.lines()[0].quantity).toBe(1);
  });

  it('remove_ultimoItem_liberaODia', () => {
    cart.add(FEIJOADA);
    cart.remove(1);

    expect(cart.isEmpty()).toBe(true);
    expect(cart.date()).toBeNull();
    expect(cart.add(OTHER_DAY)).toBe('added');
  });

  it('replaceWith_pedidoEnviado_substituiOCarrinho', () => {
    cart.add(FEIJOADA);

    cart.replaceWith('2026-10-29', [{ dishId: 3, dishName: 'Strogonoff', quantity: 4 }]);

    expect(cart.date()).toBe('2026-10-29');
    expect(cart.toItems()).toEqual([{ dishId: 3, quantity: 4 }]);
  });

  it('clear_comItens_esvaziaOCarrinho', () => {
    cart.add(FEIJOADA);
    cart.add(MOQUECA);

    cart.clear();

    expect(cart.isEmpty()).toBe(true);
    expect(cart.date()).toBeNull();
  });
});
